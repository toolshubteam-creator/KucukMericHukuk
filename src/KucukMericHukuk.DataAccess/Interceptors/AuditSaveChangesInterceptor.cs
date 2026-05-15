using System.Text.Json;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.DataAccess.Interceptors;

/// <summary>
/// Faz 7.1 — AppDbContext.SaveChanges sırasında otomatik audit log üretir.
/// Ignore listesindeki entity'ler (Identity, AuditLog, public form girişleri) hariç tüm
/// Added/Modified/Deleted state geçişleri AuditLog tablosuna işlenir. Defansif: audit
/// hatası asıl SaveChanges'i kırmaz — log only.
///
/// Scoped service: bir request = bir DbContext = bir interceptor instance, instance field
/// thread-safe (DbContext zaten tek thread'lik async-flow).
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    // Audit DIŞINDA tutulan entity tip adları — interceptor bunları yok sayar.
    private static readonly HashSet<string> IgnoredEntityNames = new(StringComparer.Ordinal)
    {
        "AuditLog",                       // sonsuz döngü
        // Identity:
        "ApplicationUser", "ApplicationRole",
        "IdentityUserRole", "IdentityUserClaim", "IdentityRoleClaim",
        "IdentityUserLogin", "IdentityUserToken",
        // Public form girişleri (kendi tablolarında zaten kayıtlı):
        "ContactMessage", "ContactMessageReply", "Appointment",
        "Subscriber"                      // Faz 7.2'de eklenecek; pre-emptive ignore
    };

    // Delta'da gürültü oluşturan audit alanları — yalnız bunlar değişmişse log oluşturulmaz.
    private static readonly HashSet<string> IgnoredFieldNames = new(StringComparer.Ordinal)
    {
        "CreatedAt", "UpdatedAt", "DeletedAt"
    };

    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    // Added entity'lerde PK store-generated olduğu için SavingChanges anında EntityId bilinmiyor (örn. int IDENTITY).
    // SavedChanges hook'unda real ID'lere göre fixup yapılır.
    private readonly List<(EntityEntry Entry, AuditLog Audit)> _pendingAddedAudits = new();

    public AuditSaveChangesInterceptor(
        ICurrentUserAccessor currentUser,
        ILogger<AuditSaveChangesInterceptor> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        TryWriteAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        TryWriteAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        TryFixupAddedEntityIds(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        TryFixupAddedEntityIds(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // -------------------- internal --------------------

    private void TryWriteAuditEntries(DbContext? context)
    {
        if (context is not AppDbContext db) return;
        _pendingAddedAudits.Clear();
        try
        {
            WriteAuditEntries(db);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuditSaveChangesInterceptor (SavingChanges) exception — audit kaybedildi, asıl işlem devam ediyor");
        }
    }

    private void WriteAuditEntries(AppDbContext context)
    {
        var userId = TryGetUserId();
        var userName = TryGetUserName();
        var ipAddress = TryGetIpAddress();
        var now = DateTime.UtcNow;

        // Materialize — başka entity (AuditLog) Add edince ChangeTracker değişir.
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var auditsToAdd = new List<AuditLog>();

        foreach (var entry in entries)
        {
            var entityTypeName = entry.Entity.GetType().Name;
            if (IgnoredEntityNames.Contains(entityTypeName)) continue;

            AuditActionType action;
            string? changesJson;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = AuditActionType.Created;
                    changesJson = SerializeCurrentValues(entry);
                    break;

                case EntityState.Deleted:
                    action = AuditActionType.Deleted;
                    changesJson = SerializeOriginalValues(entry);
                    break;

                case EntityState.Modified:
                    // Soft-delete transition: IsDeleted false→true → "Deleted", true→false → "Restored"
                    var softDeleteAction = TryDetectSoftDeleteTransition(entry);
                    if (softDeleteAction is AuditActionType.Deleted)
                    {
                        action = AuditActionType.Deleted;
                        changesJson = SerializeCurrentValues(entry);
                        break;
                    }
                    if (softDeleteAction is AuditActionType.Restored)
                    {
                        action = AuditActionType.Restored;
                        changesJson = null;
                        break;
                    }

                    // Normal Modified — delta sadece değişen alanlar
                    var delta = BuildDelta(entry);
                    if (delta.Count == 0) continue;  // sadece ignored field (UpdatedAt vb.) değişmiş
                    action = AuditActionType.Modified;
                    changesJson = JsonSerializer.Serialize(delta);
                    break;

                default:
                    continue;
            }

            var entityIdAtSavingTime = TryGetEntityId(entry) ?? string.Empty;
            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                EntityName = entityTypeName,
                EntityId = entityIdAtSavingTime,
                Action = action,
                ChangesJson = changesJson,
                IpAddress = ipAddress,
                CreatedAt = now
            };

            auditsToAdd.Add(audit);

            // Added state'te PK henüz store-generated değil — SavedChanges'ta fixup
            if (entry.State == EntityState.Added)
            {
                _pendingAddedAudits.Add((entry, audit));
            }
        }

        if (auditsToAdd.Count > 0)
        {
            context.AuditLogs.AddRange(auditsToAdd);
        }
    }

    private static AuditActionType? TryDetectSoftDeleteTransition(EntityEntry entry)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
        if (prop is null || !prop.IsModified) return null;

        var oldVal = prop.OriginalValue as bool? ?? false;
        var newVal = prop.CurrentValue as bool? ?? false;
        if (!oldVal && newVal) return AuditActionType.Deleted;
        if (oldVal && !newVal) return AuditActionType.Restored;
        return null;
    }

    private void TryFixupAddedEntityIds(DbContext? context)
    {
        if (_pendingAddedAudits.Count == 0) return;
        if (context is not AppDbContext db) return;

        try
        {
            foreach (var (entry, audit) in _pendingAddedAudits)
            {
                var realId = TryGetEntityId(entry);
                if (!string.IsNullOrEmpty(realId)) audit.EntityId = realId;
            }
            _pendingAddedAudits.Clear();
            // Sadece AuditLog.EntityId değişti — bu update bir kez daha SavingChanges'i tetikler,
            // ama AuditLog ignore listesinde olduğu için yeni audit üretilmez (sonsuz döngü yok).
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuditSaveChangesInterceptor post-save EntityId fixup failed — Added audit kayıtları placeholder ID ile kaldı");
            _pendingAddedAudits.Clear();
        }
    }

    private static string? TryGetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count == 0) return null;
        var pkProp = key.Properties[0];
        var value = entry.Property(pkProp.Name).CurrentValue;
        return value?.ToString();
    }

    private static string SerializeCurrentValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (IgnoredFieldNames.Contains(prop.Metadata.Name)) continue;
            dict[prop.Metadata.Name] = prop.CurrentValue;
        }
        return JsonSerializer.Serialize(dict);
    }

    private static string SerializeOriginalValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (IgnoredFieldNames.Contains(prop.Metadata.Name)) continue;
            dict[prop.Metadata.Name] = prop.OriginalValue;
        }
        return JsonSerializer.Serialize(dict);
    }

    private static Dictionary<string, object> BuildDelta(EntityEntry entry)
    {
        var delta = new Dictionary<string, object>();
        foreach (var prop in entry.Properties)
        {
            if (IgnoredFieldNames.Contains(prop.Metadata.Name)) continue;
            if (!prop.IsModified) continue;
            if (Equals(prop.OriginalValue, prop.CurrentValue)) continue;
            delta[prop.Metadata.Name] = new
            {
                old = prop.OriginalValue,
                @new = prop.CurrentValue
            };
        }
        return delta;
    }

    private int? TryGetUserId() => _currentUser.UserId;
    private string? TryGetUserName() => _currentUser.UserName;
    private string? TryGetIpAddress() => _currentUser.IpAddress;
}

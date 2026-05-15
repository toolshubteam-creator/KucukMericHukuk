using System.Text.Encodings.Web;
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
/// Faz 7.1-fix: Audit kaydı CAPTURE/COMMIT pattern ile iki fazlı.
///   • SavingChanges  → değişiklikleri yakala, JSON'u hesapla, _pending listesine al (DB'ye yazma yok)
///   • SavedChanges   → entry.Id artık gerçek değer; AuditLog satırlarını oluştur + AddRange + ikinci SaveChanges
/// Avantaj: audit satırları INSERT edildiğinde EntityId zaten gerçek değer (önceki impl: insert + post-save UPDATE).
/// İkinci SaveChanges interceptor'ı yeniden tetikler ama AuditLog ignore listesinde, sonsuz döngü yok.
///
/// Atomicity notu: audit insert ayrı transaction. Asıl save committed sonra audit insert fail olursa
/// audit kaybı tolere edilir (log only); kullanıcının işlemi etkilenmez.
/// Scoped service: bir request = bir DbContext = bir interceptor instance, _pending field thread-safe.
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

    // Türkçe karakterler (Ö, ü vb.) JSON'da \uXXXX escape edilmeden saklanır — admin Details
    // sayfası "Ham JSON" bloğunda okunabilir kalsın diye. Render güvenli (HtmlEncode + <pre>).
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    /// <summary>SavingChanges aşamasında yakalanan, SavedChanges'te commit edilecek geçici audit veri.</summary>
    private record PendingAudit(
        EntityEntry Entry,
        AuditActionType Action,
        string? ChangesJson);

    private readonly List<PendingAudit> _pending = new();

    public AuditSaveChangesInterceptor(
        ICurrentUserAccessor currentUser,
        ILogger<AuditSaveChangesInterceptor> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    // -------------------- SAVING (capture) --------------------

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        TryCaptureChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        TryCaptureChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // -------------------- SAVED (commit) --------------------

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        TryCommitAudit(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        await TryCommitAuditAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // -------------------- internal --------------------

    private void TryCaptureChanges(DbContext? context)
    {
        _pending.Clear();
        if (context is not AppDbContext db) return;
        try
        {
            CaptureChanges(db);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuditSaveChangesInterceptor capture exception — audit kaybedildi, asıl işlem devam ediyor");
            _pending.Clear();
        }
    }

    private void CaptureChanges(AppDbContext context)
    {
        // Materialize — başka entity Add edince ChangeTracker değişir.
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

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
                    // Snapshot SavingChanges'te alınır — PK alanı temp değer (örn. 0 ya da negatif int)
                    // içerebilir, ama AuditLog.EntityId kolonu SavedChanges'te gerçek ID ile dolar.
                    changesJson = SerializeCurrentValues(entry);
                    break;

                case EntityState.Deleted:
                    // Hard delete — entry SavedChanges sonrası Detached, snapshot ŞİMDİ alınmalı.
                    action = AuditActionType.Deleted;
                    changesJson = SerializeOriginalValues(entry);
                    break;

                case EntityState.Modified:
                    // Soft-delete: IsDeleted false→true → "Deleted", true→false → "Restored"
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

                    // Normal Modified — delta sadece değişen alanlar.
                    // Delta OriginalValues gerektirir; SavingChanges'te hesaplanmalı (post-save'de OriginalValues = CurrentValues).
                    var delta = BuildDelta(entry);
                    if (delta.Count == 0) continue;  // sadece ignored field (UpdatedAt vb.) değişmiş
                    action = AuditActionType.Modified;
                    changesJson = JsonSerializer.Serialize(delta, JsonOpts);
                    break;

                default:
                    continue;
            }

            _pending.Add(new PendingAudit(entry, action, changesJson));
        }
    }

    private void TryCommitAudit(DbContext? context)
    {
        if (_pending.Count == 0) return;
        if (context is not AppDbContext db) return;

        try
        {
            var audits = BuildAuditLogsFromPending();
            _pending.Clear();
            if (audits.Count == 0) return;

            db.AuditLogs.AddRange(audits);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuditSaveChangesInterceptor commit failed — audit kayıtları yazılamadı, asıl işlem etkilenmedi");
            _pending.Clear();
        }
    }

    private async ValueTask TryCommitAuditAsync(DbContext? context, CancellationToken ct)
    {
        if (_pending.Count == 0) return;
        if (context is not AppDbContext db) return;

        try
        {
            var audits = BuildAuditLogsFromPending();
            _pending.Clear();
            if (audits.Count == 0) return;

            db.AuditLogs.AddRange(audits);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuditSaveChangesInterceptor commit (async) failed — audit kayıtları yazılamadı, asıl işlem etkilenmedi");
            _pending.Clear();
        }
    }

    /// <summary>
    /// SavedChanges aşamasında çağrılır — Added entry'lerin EntityId'si artık DB-generated gerçek değer.
    /// Bu yüzden audit objesini ŞİMDİ inşa ederiz, EntityId temp olmayan değeri yansıtır.
    /// </summary>
    private List<AuditLog> BuildAuditLogsFromPending()
    {
        var userId = _currentUser.UserId;
        var userName = _currentUser.UserName;
        var ipAddress = _currentUser.IpAddress;
        var now = DateTime.UtcNow;

        var audits = new List<AuditLog>(_pending.Count);
        foreach (var p in _pending)
        {
            // Detached entry (hard delete sonrası) için TryGetEntityId hâlâ OriginalValues'tan
            // veya entity instance'ından okuyabilir — defansif null check.
            var entityId = TryGetEntityId(p.Entry) ?? string.Empty;

            audits.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                EntityName = p.Entry.Entity.GetType().Name,
                EntityId = entityId,
                Action = p.Action,
                ChangesJson = p.ChangesJson,
                IpAddress = ipAddress,
                CreatedAt = now
            });
        }
        return audits;
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

    private static string? TryGetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count == 0) return null;
        var pkProp = key.Properties[0];
        try
        {
            var value = entry.Property(pkProp.Name).CurrentValue;
            return value?.ToString();
        }
        catch
        {
            // Detached entry (örn. hard delete sonrası) Property erişimi fırlatabilir;
            // entity instance'ından reflection ile fallback.
            var clrProp = entry.Entity.GetType().GetProperty(pkProp.Name);
            return clrProp?.GetValue(entry.Entity)?.ToString();
        }
    }

    private static string SerializeCurrentValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (IgnoredFieldNames.Contains(prop.Metadata.Name)) continue;
            dict[prop.Metadata.Name] = prop.CurrentValue;
        }
        return JsonSerializer.Serialize(dict, JsonOpts);
    }

    private static string SerializeOriginalValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (IgnoredFieldNames.Contains(prop.Metadata.Name)) continue;
            dict[prop.Metadata.Name] = prop.OriginalValue;
        }
        return JsonSerializer.Serialize(dict, JsonOpts);
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
}

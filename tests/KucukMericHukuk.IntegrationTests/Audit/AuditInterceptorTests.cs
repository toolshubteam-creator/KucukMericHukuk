using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Audit;

/// <summary>
/// Faz 7.1 — AuditSaveChangesInterceptor entegrasyon testleri.
/// IntegrationTestFactory'de DbContext interceptor ile wire edilmiş; bu testler
/// gerçek SaveChanges akışında audit kayıt üretildiğini doğrular.
/// </summary>
public class AuditInterceptorTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AuditInterceptorTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task ClearAuditLogsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AuditLogs.RemoveRange(db.AuditLogs.ToList());
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AddTestimonial_GeneratesCreatedAuditRecord()
    {
        await ClearAuditLogsAsync();

        int testimonialId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = new Testimonial { AuthorInitials = "A.B.", AuthorRole = "Müvekkil", IsActive = true };
            db.Set<Testimonial>().Add(t);
            await db.SaveChangesAsync();
            testimonialId = t.Id;
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await verifyDb.AuditLogs
            .Where(a => a.EntityName == "Testimonial")
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AuditActionType.Created);
        audit.EntityId.Should().Be(testimonialId.ToString(), "SavedChanges'te gerçek DB ID yazılmalı (capture/commit pattern)");

        // Faz 7.1-fix: EntityId temp negatif değer DEĞİL — gerçek pozitif DB ID
        var parsedId = int.Parse(audit.EntityId);
        parsedId.Should().BePositive("EF Core SQLite/SQL Server temp ID'leri (0 veya negatif) audit'e SIZMAMALI");
        parsedId.Should().Be(testimonialId);

        audit.ChangesJson.Should().NotBeNullOrEmpty();
        audit.ChangesJson.Should().Contain("AuthorInitials");

        // Faz 7.1-fix2: snapshot içindeki "Id" alanı da DB-generated gerçek değer olmalı,
        // SavingChanges sırasındaki temp ID (0 / negatif) DEĞİL.
        var snapshotIdInJson = ExtractIdFromJson(audit.ChangesJson!);
        snapshotIdInJson.Should().Be(testimonialId, "ChangesJson içindeki Id alanı post-save gerçek değer olmalı");
        snapshotIdInJson.Should().BePositive();
    }

    private static int ExtractIdFromJson(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Id").GetInt32();
    }

    [Fact]
    public async Task CreateMultipleEntities_AllAuditLogsHaveRealIds()
    {
        // Faz 7.1-fix: tek SaveChanges'te birden fazla entity Add edilirse hepsinin
        // audit EntityId'si gerçek/pozitif olmalı — temp negative ID sızmamalı.
        await ClearAuditLogsAsync();

        var createdIds = new List<int>();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t1 = new Testimonial { AuthorInitials = "M.1", AuthorRole = "Müvekkil", IsActive = true };
            var t2 = new Testimonial { AuthorInitials = "M.2", AuthorRole = "Müvekkil", IsActive = true };
            var t3 = new Testimonial { AuthorInitials = "M.3", AuthorRole = "Müvekkil", IsActive = true };
            db.Set<Testimonial>().AddRange(t1, t2, t3);
            await db.SaveChangesAsync();
            createdIds.AddRange(new[] { t1.Id, t2.Id, t3.Id });
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await verifyDb.AuditLogs
            .Where(a => a.EntityName == "Testimonial" && a.Action == AuditActionType.Created)
            .ToListAsync();

        audits.Should().HaveCount(3);
        var auditEntityIds = audits.Select(a => int.Parse(a.EntityId)).OrderBy(x => x).ToList();
        var expectedIds = createdIds.OrderBy(x => x).ToList();

        auditEntityIds.Should().BeEquivalentTo(expectedIds, "her audit row gerçek entity Id'sini içermeli");
        auditEntityIds.Should().OnlyContain(id => id > 0, "hiçbir audit temp/negative ID içermemeli");

        // Faz 7.1-fix2: her snapshot içindeki "Id" alanı da pozitif/gerçek değer
        var snapshotIds = audits.Select(a => ExtractIdFromJson(a.ChangesJson!)).OrderBy(x => x).ToList();
        snapshotIds.Should().BeEquivalentTo(expectedIds, "her ChangesJson içindeki Id alanı post-save gerçek değer olmalı");
        snapshotIds.Should().OnlyContain(id => id > 0);
    }

    [Fact]
    public async Task ModifyEntity_ChangesJsonDeltaUntouchedByPkPatch()
    {
        // Faz 7.1-fix2: PK patch sadece Created için çalışır; Modified delta'nın
        // alan yapısı bozulmamalı (PK delta'da zaten yok, ama defansif test).
        await ClearAuditLogsAsync();

        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = new Testimonial { AuthorInitials = "P.T.", AuthorRole = "Müvekkil", IsActive = true };
            db.Set<Testimonial>().Add(t);
            await db.SaveChangesAsync();
            id = t.Id;
        }

        await ClearAuditLogsAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = await db.Set<Testimonial>().FirstAsync(x => x.Id == id);
            t.AuthorRole = "Eski Müvekkil";
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await verifyDb.AuditLogs
            .Where(a => a.EntityName == "Testimonial" && a.Action == AuditActionType.Modified)
            .FirstAsync();

        // Modified delta yapısı: { AuthorRole: { old, new } }
        using var doc = System.Text.Json.JsonDocument.Parse(audit.ChangesJson!);
        doc.RootElement.TryGetProperty("AuthorRole", out var roleNode).Should().BeTrue();
        roleNode.TryGetProperty("old", out _).Should().BeTrue();
        roleNode.TryGetProperty("new", out _).Should().BeTrue();
        // PK delta'da olmamalı (Modified'da PK değişmiyor)
        doc.RootElement.TryGetProperty("Id", out _).Should().BeFalse("Modified delta PK içermez, patch dokunmamalı");
    }

    [Fact]
    public async Task ModifyTestimonial_GeneratesModifiedAuditWithDelta()
    {
        await ClearAuditLogsAsync();

        // Seed
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = new Testimonial { AuthorInitials = "C.D.", AuthorRole = "Müvekkil", IsActive = true };
            db.Set<Testimonial>().Add(t);
            await db.SaveChangesAsync();
            id = t.Id;
        }

        await ClearAuditLogsAsync(); // Created audit'i temizle, sadece Modified'i izle

        // Modify
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = await db.Set<Testimonial>().FirstAsync(x => x.Id == id);
            t.AuthorRole = "Önceki Müvekkil";
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await verifyDb.AuditLogs
            .Where(a => a.EntityName == "Testimonial" && a.Action == AuditActionType.Modified)
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull("AuthorRole değişikliği audit'lenmeli");
        audit!.ChangesJson.Should().NotBeNullOrEmpty();
        audit.ChangesJson.Should().Contain("AuthorRole");
        audit.ChangesJson.Should().Contain("Önceki Müvekkil", "delta yeni değeri içermeli");
        // Delta SADECE değişen alanı içermeli — AuthorInitials/IsActive olmamalı
        audit.ChangesJson.Should().NotContain("AuthorInitials");
        audit.ChangesJson.Should().NotContain("IsActive");
    }

    [Fact]
    public async Task SoftDeleteTestimonial_GeneratesDeletedAuditNotModified()
    {
        await ClearAuditLogsAsync();

        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = new Testimonial { AuthorInitials = "E.F.", AuthorRole = "Müvekkil", IsActive = true };
            db.Set<Testimonial>().Add(t);
            await db.SaveChangesAsync();
            id = t.Id;
        }

        await ClearAuditLogsAsync();

        // Soft-delete (IsDeleted false→true)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = await db.Set<Testimonial>().FirstAsync(x => x.Id == id);
            t.IsDeleted = true;
            t.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await verifyDb.AuditLogs.Where(a => a.EntityName == "Testimonial").ToListAsync();

        audits.Should().HaveCount(1);
        audits[0].Action.Should().Be(AuditActionType.Deleted, "soft-delete logical Deleted olarak audit'lenmeli, Modified DEĞİL");
    }

    [Fact]
    public async Task RestoreTestimonial_GeneratesRestoredAudit()
    {
        await ClearAuditLogsAsync();

        // Seed soft-deleted
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = new Testimonial
            {
                AuthorInitials = "G.H.", AuthorRole = "Müvekkil", IsActive = true,
                IsDeleted = true, DeletedAt = DateTime.UtcNow
            };
            db.Set<Testimonial>().Add(t);
            await db.SaveChangesAsync();
            id = t.Id;
        }

        await ClearAuditLogsAsync();

        // Restore (IsDeleted true→false) — query filter bypass için IgnoreQueryFilters
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = await db.Set<Testimonial>().IgnoreQueryFilters().FirstAsync(x => x.Id == id);
            t.IsDeleted = false;
            t.DeletedAt = null;
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await verifyDb.AuditLogs
            .Where(a => a.EntityName == "Testimonial")
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AuditActionType.Restored);
    }

    [Fact]
    public async Task ContactMessage_IgnoredFromAudit()
    {
        await ClearAuditLogsAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var msg = new ContactMessage
            {
                Name = "Test", Email = "test@example.com", Subject = "S", Message = "M",
                KvkkConsent = true, CreatedAt = DateTime.UtcNow
            };
            db.Set<ContactMessage>().Add(msg);
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await verifyDb.AuditLogs.Where(a => a.EntityName == "ContactMessage").ToListAsync();

        audits.Should().BeEmpty("ContactMessage ignore listesinde — audit oluşmamalı");
    }

    [Fact]
    public async Task ApplicationUser_IgnoredFromAudit()
    {
        await ClearAuditLogsAsync();

        // Seed yapılan kullanıcı zaten var; sadece bir Update tetikleyip audit olup olmadığına bakalım
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync();
            user.PhoneNumber = "5551112233";
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await verifyDb.AuditLogs.Where(a => a.EntityName == "ApplicationUser").ToListAsync();

        audits.Should().BeEmpty("Identity entity'leri ignore listesinde");
    }

    [Fact]
    public async Task SavingAuditLogItself_NoInfiniteLoop()
    {
        // Bir Testimonial işlemi 1 audit üretmeli (Created). İkinci audit (audit kaydını
        // audit'lemek) OLMAMALI — interceptor AuditLog ignore listesinde olduğu için.
        await ClearAuditLogsAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<Testimonial>().Add(new Testimonial
            {
                AuthorInitials = "Y.Z.", AuthorRole = "Müvekkil", IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var testimonialAudits = await verifyDb.AuditLogs.Where(a => a.EntityName == "Testimonial").CountAsync();
        var auditAudits = await verifyDb.AuditLogs.Where(a => a.EntityName == "AuditLog").CountAsync();

        testimonialAudits.Should().Be(1, "tek Created audit beklenir");
        auditAudits.Should().Be(0, "audit kaydı kendisi audit'lenmez (sonsuz döngü guard)");
    }

    [Fact]
    public async Task ModifyOnlyAuditFields_NoAuditCreated()
    {
        await ClearAuditLogsAsync();

        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = new Testimonial { AuthorInitials = "I.K.", AuthorRole = "Müvekkil", IsActive = true };
            db.Set<Testimonial>().Add(t);
            await db.SaveChangesAsync();
            id = t.Id;
        }

        await ClearAuditLogsAsync();

        // Sadece UpdatedAt değişikliği — delta'da gürültü olmamalı, audit OLUŞMAMALI
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = await db.Set<Testimonial>().FirstAsync(x => x.Id == id);
            t.UpdatedAt = DateTime.UtcNow.AddMinutes(5);
            await db.SaveChangesAsync();
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await verifyDb.AuditLogs.Where(a => a.EntityName == "Testimonial").ToListAsync();

        audits.Should().BeEmpty("sadece audit alan (UpdatedAt) değişikliği audit oluşturmamalı");
    }
}

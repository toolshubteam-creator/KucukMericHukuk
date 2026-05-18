using FluentAssertions;
using KucukMericHukuk.Core.DTOs.NotFoundLog;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Repositories;
using KucukMericHukuk.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.DataAccess;

/// <summary>
/// Faz 7.3.1 — NotFoundLogRepository aggregate davranışı.
/// SQLite in-memory; ExecuteUpdateAsync ve unique constraint SQLite tarafından desteklenir.
/// </summary>
public class NotFoundLogRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public NotFoundLogRepositoryTests()
    {
        _factory = new TestDbContextFactory();
    }

    [Fact]
    public async Task RecordHitAsync_NewUrl_CreatesSingleRowWithHitCount1()
    {
        await using var context = _factory.CreateContext();
        var repo = new NotFoundLogRepository(context);

        var inserted = await repo.RecordHitAsync(
            "/tr-TR/eski-sayfa",
            "https://google.com",
            "Mozilla/5.0",
            "127.0.0.1");

        inserted.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var rows = await verify.Set<NotFoundLog>().ToListAsync();
        rows.Should().HaveCount(1);
        rows[0].Url.Should().Be("/tr-TR/eski-sayfa");
        rows[0].HitCount.Should().Be(1);
        rows[0].Referer.Should().Be("https://google.com");
        rows[0].UserAgent.Should().Be("Mozilla/5.0");
        rows[0].IpAddress.Should().Be("127.0.0.1");
        rows[0].FirstSeenAt.Should().Be(rows[0].LastSeenAt);
    }

    [Fact]
    public async Task RecordHitAsync_SameUrlTwice_KeepsSingleRowAndIncrementsHitCount()
    {
        await using var context = _factory.CreateContext();
        var repo = new NotFoundLogRepository(context);

        var firstInserted = await repo.RecordHitAsync("/tr-TR/x", null, null, "1.1.1.1");
        // Saat farkını gözle görür yap (LastSeenAt değişimini doğrulamak için).
        await Task.Delay(10);
        var secondInserted = await repo.RecordHitAsync("/tr-TR/x", "ref2", "ua2", "2.2.2.2");

        firstInserted.Should().BeTrue();
        secondInserted.Should().BeFalse();

        await using var verify = _factory.CreateContext();
        var rows = await verify.Set<NotFoundLog>().ToListAsync();
        rows.Should().HaveCount(1, "aggregate: aynı URL = tek satır");
        rows[0].HitCount.Should().Be(2);
        rows[0].LastSeenAt.Should().BeAfter(rows[0].FirstSeenAt, "LastSeenAt UPDATE'te güncellendi");
        // İlk hit'in context'i (Referer/UA/IP) korunur — sonraki hit'lerde dokunulmaz.
        rows[0].Referer.Should().BeNull();
        rows[0].UserAgent.Should().BeNull();
        rows[0].IpAddress.Should().Be("1.1.1.1");
    }

    [Fact]
    public async Task RecordHitAsync_DifferentUrls_CreatesSeparateRows()
    {
        await using var context = _factory.CreateContext();
        var repo = new NotFoundLogRepository(context);

        await repo.RecordHitAsync("/tr-TR/a", null, null, null);
        await repo.RecordHitAsync("/tr-TR/b", null, null, null);
        await repo.RecordHitAsync("/tr-TR/a", null, null, null);

        await using var verify = _factory.CreateContext();
        var rows = await verify.Set<NotFoundLog>().OrderBy(n => n.Url).ToListAsync();
        rows.Should().HaveCount(2);
        rows[0].Url.Should().Be("/tr-TR/a");
        rows[0].HitCount.Should().Be(2);
        rows[1].Url.Should().Be("/tr-TR/b");
        rows[1].HitCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminPagedAsync_DefaultSort_OrdersByHitCountDescending()
    {
        await using var context = _factory.CreateContext();
        var repo = new NotFoundLogRepository(context);
        await repo.RecordHitAsync("/url-1-hit", null, null, null);
        await repo.RecordHitAsync("/url-3-hits", null, null, null);
        await repo.RecordHitAsync("/url-3-hits", null, null, null);
        await repo.RecordHitAsync("/url-3-hits", null, null, null);
        await repo.RecordHitAsync("/url-2-hits", null, null, null);
        await repo.RecordHitAsync("/url-2-hits", null, null, null);

        var result = await repo.GetAdminPagedAsync(new NotFoundLogQueryDto());

        result.TotalCount.Should().Be(3);
        result.Items[0].Url.Should().Be("/url-3-hits");
        result.Items[1].Url.Should().Be("/url-2-hits");
        result.Items[2].Url.Should().Be("/url-1-hit");
    }

    [Fact]
    public async Task GetAdminPagedAsync_KeywordFilter_MatchesUrlSubstring()
    {
        await using var context = _factory.CreateContext();
        var repo = new NotFoundLogRepository(context);
        await repo.RecordHitAsync("/tr-TR/makaleler/eski", null, null, null);
        await repo.RecordHitAsync("/tr-TR/avukatlar/silinmis", null, null, null);
        await repo.RecordHitAsync("/tr-TR/makaleler/baska", null, null, null);

        var result = await repo.GetAdminPagedAsync(new NotFoundLogQueryDto { Keyword = "makaleler" });

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(n => n.Url.Contains("makaleler"));
    }

    [Fact]
    public async Task GetAdminPagedAsync_SortRecent_OrdersByLastSeenAtDescending()
    {
        await using var context = _factory.CreateContext();
        var repo = new NotFoundLogRepository(context);
        await repo.RecordHitAsync("/url-old-but-hits-3", null, null, null);
        await repo.RecordHitAsync("/url-old-but-hits-3", null, null, null);
        await repo.RecordHitAsync("/url-old-but-hits-3", null, null, null);
        await Task.Delay(20);
        await repo.RecordHitAsync("/url-recent-1-hit", null, null, null);

        var result = await repo.GetAdminPagedAsync(new NotFoundLogQueryDto { Sort = "recent" });

        result.Items[0].Url.Should().Be("/url-recent-1-hit");
        result.Items[1].Url.Should().Be("/url-old-but-hits-3");
    }

    /// <summary>
    /// Faz 7.3.1 — NotFoundLog audit ignore listesinde. RecordHitAsync sonrası
    /// AuditLogs tablosunda "NotFoundLog" entity_name'li satır olmamalı (ContactMessage/Subscriber gibi).
    /// </summary>
    [Fact]
    public async Task RecordHitAsync_DoesNotProduceAuditLog()
    {
        // Audit interceptor yok bu context'te (TestDbContextFactory minimal) —
        // pattern doğrulaması: ignore listesine "NotFoundLog" eklenmiş olduğunu
        // Interceptor.IgnoredEntityNames içeriği üzerinden doğrularız.
        var ignoredField = typeof(KucukMericHukuk.DataAccess.Interceptors.AuditSaveChangesInterceptor)
            .GetField("IgnoredEntityNames",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        ignoredField.Should().NotBeNull();
        var ignored = (HashSet<string>)ignoredField!.GetValue(null)!;
        ignored.Should().Contain("NotFoundLog",
            "audit interceptor 404 log'larini sistem-uretimi olarak gormeli, ContactMessage/Subscriber gibi");
    }

    public void Dispose() => _factory.Dispose();
}

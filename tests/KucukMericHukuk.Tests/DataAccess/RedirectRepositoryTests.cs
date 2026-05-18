using FluentAssertions;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Repositories;
using KucukMericHukuk.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.DataAccess;

/// <summary>
/// Faz 7.4.1 — RedirectRepository: GetByFromPath (sadece aktif), atomic HitCount,
/// duplicate check, admin paged filter.
/// </summary>
public class RedirectRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public RedirectRepositoryTests()
    {
        _factory = new TestDbContextFactory();
    }

    private static Redirect BuildRedirect(
        string from,
        string to = "/yeni",
        bool isActive = true,
        int statusCode = 301)
    {
        return new Redirect
        {
            FromPath = from,
            ToPath = to,
            StatusCode = statusCode,
            IsActive = isActive,
            HitCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetByFromPathAsync_ActiveExists_ReturnsRedirect()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(BuildRedirect("/eski-link", "/yeni-link"));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        var result = await repo.GetByFromPathAsync("/eski-link");

        result.Should().NotBeNull();
        result!.ToPath.Should().Be("/yeni-link");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByFromPathAsync_InactiveRedirect_ReturnsNull()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(BuildRedirect("/pasif", isActive: false));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        var result = await repo.GetByFromPathAsync("/pasif");

        result.Should().BeNull("middleware pasif redirect'leri atlamalı");
    }

    [Fact]
    public async Task RecordHitAsync_IncrementsHitCountAtomically()
    {
        int redirectId;
        await using (var seed = _factory.CreateContext())
        {
            var r = BuildRedirect("/x");
            seed.Redirects.Add(r);
            await seed.SaveChangesAsync();
            redirectId = r.Id;
        }

        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        var first = await repo.RecordHitAsync(redirectId);
        var second = await repo.RecordHitAsync(redirectId);

        first.Should().BeTrue();
        second.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var saved = await verify.Redirects.FirstAsync(r => r.Id == redirectId);
        saved.HitCount.Should().Be(2);
        saved.LastHitAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordHitAsync_MissingId_ReturnsFalse()
    {
        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        var result = await repo.RecordHitAsync(99999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsFromPathAsync_Duplicate_ReturnsTrue()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(BuildRedirect("/test"));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        (await repo.ExistsFromPathAsync("/test")).Should().BeTrue();
        (await repo.ExistsFromPathAsync("/yok")).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsFromPathAsync_WithExcludeId_IgnoresSelf()
    {
        int rid;
        await using (var seed = _factory.CreateContext())
        {
            var r = BuildRedirect("/test");
            seed.Redirects.Add(r);
            await seed.SaveChangesAsync();
            rid = r.Id;
        }

        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        // Kendi id'sini hariç tutarsak duplicate görünmez (edit senaryosu).
        (await repo.ExistsFromPathAsync("/test", excludeId: rid)).Should().BeFalse();
    }

    [Fact]
    public async Task GetAdminPagedAsync_KeywordFiltersFromAndToPaths()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.AddRange(
                BuildRedirect("/eski-makale-1", "/yeni-makale-1"),
                BuildRedirect("/eski-avukat", "/avukatlar/listele"),
                BuildRedirect("/silinmis", "/anasayfa"));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        var result = await repo.GetAdminPagedAsync(new RedirectQueryDto { Keyword = "makale" });

        result.TotalCount.Should().Be(1);
        result.Items[0].FromPath.Should().Be("/eski-makale-1");
    }

    [Fact]
    public async Task GetAdminPagedAsync_IsActiveFilter_Works()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.AddRange(
                BuildRedirect("/a", isActive: true),
                BuildRedirect("/b", isActive: false),
                BuildRedirect("/c", isActive: true));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new RedirectRepository(context);

        var active = await repo.GetAdminPagedAsync(new RedirectQueryDto { IsActive = true });
        active.TotalCount.Should().Be(2);

        var inactive = await repo.GetAdminPagedAsync(new RedirectQueryDto { IsActive = false });
        inactive.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Faz 7.4.1 — Redirect audit ignore listesinde DEĞİL. Admin CRUD denetlenir.
    /// SlugHistory ignore'da (sistem üretir, ayrı entity).
    /// </summary>
    [Fact]
    public void Redirect_IsNotInAuditIgnoreList_ButSlugHistoryIs()
    {
        var ignoredField = typeof(KucukMericHukuk.DataAccess.Interceptors.AuditSaveChangesInterceptor)
            .GetField("IgnoredEntityNames",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        ignoredField.Should().NotBeNull();
        var ignored = (HashSet<string>)ignoredField!.GetValue(null)!;

        ignored.Should().NotContain("Redirect",
            "Redirect admin CRUD audit'e dusmeli - kim ne redirect kurmus izlenebilir");
        ignored.Should().Contain("SlugHistory",
            "SlugHistory sistem-uretimi (NotFoundLog gibi) - audit gurultusu olmasin");
    }

    public void Dispose() => _factory.Dispose();
}

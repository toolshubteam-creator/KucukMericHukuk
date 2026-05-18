using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Middleware;

/// <summary>
/// Faz 7.4.2 — RedirectMiddleware POC.
/// KRİTİK ASIL ÖLÇÜ:
///   (a) Aktif redirect → 301/302 + Location header doğru
///   (b) O redirect URL'i NotFoundLog'a DÜŞMÜYOR (7.3 ile çakışma sıfır, pipeline order kanıtı)
///   (c) Redirect yok → NotFoundLogging akışı bozulmuyor (7.3.2 regresyon)
/// </summary>
public class RedirectMiddlewareTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public RedirectMiddlewareTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task ResetTablesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.NotFoundLogs.RemoveRange(db.NotFoundLogs.ToList());
        db.Redirects.RemoveRange(db.Redirects.ToList());
        db.SlugHistories.RemoveRange(db.SlugHistories.ToList());
        // ArticleTranslation/Article seed temizliği — sadece testlerde eklediklerimiz
        db.Set<ArticleTranslation>().RemoveRange(db.Set<ArticleTranslation>().IgnoreQueryFilters().ToList());
        db.Set<Article>().RemoveRange(db.Set<Article>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();
    }

    private async Task<List<NotFoundLog>> ReadNotFoundLogsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.NotFoundLogs.AsNoTracking().ToListAsync();
    }

    private async Task<Redirect> SeedRedirectAsync(
        string from, string to, int statusCode = 301, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var r = new Redirect
        {
            FromPath = from,
            ToPath = to,
            StatusCode = statusCode,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        db.Redirects.Add(r);
        await db.SaveChangesAsync();
        return r;
    }

    private async Task<int> SeedArticleAsync(string slug, bool isDeleted = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = new Article
        {
            Status = ArticleStatus.Published,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            Translations = new List<ArticleTranslation>
            {
                new()
                {
                    LanguageCode = "tr-TR",
                    Title = "Test",
                    Slug = slug,
                    Content = "<p>x</p>",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        db.Set<Article>().Add(article);
        await db.SaveChangesAsync();
        return article.Id;
    }

    private async Task SeedSlugHistoryAsync(int articleId, string oldSlug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SlugHistories.Add(new SlugHistory
        {
            EntityType = SluggedEntityType.Article,
            EntityId = articleId,
            LanguageCode = "tr-TR",
            OldSlug = oldSlug,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// POC ASIL TESTI (a) + (b): aktif redirect 301 dönüyor + NotFoundLog'a DÜŞMÜYOR.
    /// Pipeline order kanıtı — RedirectMiddleware NotFoundLogging'den ÖNCE.
    /// </summary>
    [Fact]
    public async Task Active_redirect_returns_301_AND_does_not_log_to_NotFoundLog()
    {
        await ResetTablesAsync();
        await SeedRedirectAsync("/tr-TR/eski-link", "/tr-TR/yeni-link", statusCode: 301);

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/tr-TR/eski-link");

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be("/tr-TR/yeni-link");

        var logs = await ReadNotFoundLogsAsync();
        logs.Should().BeEmpty(
            "RedirectMiddleware NotFoundLogging'ten ONCE - redirect URL 404 log'a DUSMEMELI");
    }

    /// <summary>
    /// POC TESTI (c): redirect yok → request controller'a ulaşır, 404 üretilir,
    /// NotFoundLogging onu kaydeder. 7.3.2 akışı bozulmadığı doğrulanır.
    /// </summary>
    [Fact]
    public async Task No_redirect_falls_through_to_NotFoundLogging()
    {
        await ResetTablesAsync();

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/hic-bir-sey-yok-burada");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var logs = await ReadNotFoundLogsAsync();
        logs.Should().HaveCount(1, "redirect yoksa NotFoundLogging akisi devam etmeli");
        logs[0].Url.Should().Be("/tr-TR/hic-bir-sey-yok-burada");
    }

    [Fact]
    public async Task Redirect_with_302_returns_temporary_redirect()
    {
        await ResetTablesAsync();
        await SeedRedirectAsync("/tr-TR/gecici", "/tr-TR/yeni-gecici", statusCode: 302);

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/tr-TR/gecici");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().Be("/tr-TR/yeni-gecici");
    }

    [Fact]
    public async Task Inactive_redirect_is_bypassed()
    {
        await ResetTablesAsync();
        await SeedRedirectAsync("/tr-TR/pasif", "/tr-TR/yeni", isActive: false);

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/tr-TR/pasif");

        // IsActive=false → middleware atlar, controller 404 verir, NotFoundLogging kaydeder
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var logs = await ReadNotFoundLogsAsync();
        logs.Should().HaveCount(1, "pasif redirect bypass edilir, NotFound akisi devam eder");
    }

    [Fact]
    public async Task SlugHistory_redirect_to_current_slug()
    {
        await ResetTablesAsync();
        var articleId = await SeedArticleAsync(slug: "yeni-makale");
        await SeedSlugHistoryAsync(articleId, oldSlug: "eski-makale");

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/tr-TR/Articles/eski-makale");

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be("/tr-TR/Articles/yeni-makale");

        var logs = await ReadNotFoundLogsAsync();
        logs.Should().BeEmpty("SlugHistory ile yonlendirilen URL NotFoundLog'a dusmemeli");
    }

    /// <summary>
    /// Soft-deleted entity bypass — SlugHistory satırı var ama parent Article IsDeleted.
    /// Query filter parent'ı gizler → current slug null → redirect ETME, 404'e bırak.
    /// </summary>
    [Fact]
    public async Task SlugHistory_for_soft_deleted_entity_does_not_redirect()
    {
        await ResetTablesAsync();
        var articleId = await SeedArticleAsync(slug: "yine-yeni", isDeleted: true);
        await SeedSlugHistoryAsync(articleId, oldSlug: "eski-silinmis");

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/tr-TR/Articles/eski-silinmis");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "soft-deleted entity icin SlugHistory varsa bile redirect yapilmaz - 404 verilir");
    }

    /// <summary>
    /// Faz 7.4.2 — Runtime self-redirect guard. FromPath==ToPath olan kayıt
    /// pass-through edilir, sonsuz tarayıcı döngüsü engellenir.
    /// </summary>
    [Fact]
    public async Task Self_redirect_FromEqualsTo_is_bypassed()
    {
        await ResetTablesAsync();
        await SeedRedirectAsync("/tr-TR/kendine", "/tr-TR/kendine");

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/tr-TR/kendine");

        // Self-loop → pass-through → controller 404 → NotFoundLogging kaydeder.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "self-redirect kayidi pass-through edilmeli, sonsuz dongu olmamali");
    }

    /// <summary>
    /// Faz 7.4.2 — IMemoryCache POSITIVE lookup sonuçlarını cache'liyor (5dk TTL).
    /// İlk request sonrası cache anahtarı popüle olmuş olmalı.
    /// </summary>
    [Fact]
    public async Task Manual_redirect_lookup_populates_cache()
    {
        await ResetTablesAsync();
        await SeedRedirectAsync("/tr-TR/cache-test", "/tr-TR/yeni-cache-hedefi");

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // İlk istek — DB lookup + cache set
        var response = await client.GetAsync("/tr-TR/cache-test");
        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);

        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();

        var hit = cache.TryGetValue<Redirect>(
            KucukMericHukuk.Web.Middleware.RedirectMiddleware.CacheKeyPrefix + "/tr-TR/cache-test",
            out var cached);
        hit.Should().BeTrue("manuel redirect lookup sonrasi POSITIVE cache populated olmali");
        cached!.ToPath.Should().Be("/tr-TR/yeni-cache-hedefi");
    }

    [Fact]
    public async Task Redirect_increments_hit_count_atomically()
    {
        await ResetTablesAsync();
        var redirect = await SeedRedirectAsync("/tr-TR/sayilan", "/tr-TR/yeni");

        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await client.GetAsync("/tr-TR/sayilan");
        await client.GetAsync("/tr-TR/sayilan");
        await client.GetAsync("/tr-TR/sayilan");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Redirects.AsNoTracking().FirstAsync(r => r.Id == redirect.Id);
        saved.HitCount.Should().Be(3);
        saved.LastHitAt.Should().NotBeNull();
    }
}

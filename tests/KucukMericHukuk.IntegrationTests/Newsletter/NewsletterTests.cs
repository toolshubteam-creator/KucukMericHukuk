using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Newsletter;

public class NewsletterTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public NewsletterTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task ClearArticlesAndJobsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<NewsletterJob>().RemoveRange(db.Set<NewsletterJob>().IgnoreQueryFilters().ToList());
        db.Set<Article>().RemoveRange(db.Set<Article>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedPublishedArticleAsync(string title = "Test", DateTime? newsletterSentAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = new Article
        {
            Status = ArticleStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            NewsletterSentAt = newsletterSentAt,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        article.Translations.Add(new ArticleTranslation
        {
            LanguageCode = LanguageCodes.Default,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(" ", "-"),
            Excerpt = "Ozet",
            Content = "Icerik"
        });
        db.Set<Article>().Add(article);
        await db.SaveChangesAsync();
        return article.Id;
    }

    [Fact]
    public async Task AdminNewsletterIndex_Authorized_ReturnsOkWithPendingArticle()
    {
        await ClearArticlesAndJobsAsync();
        await SeedPublishedArticleAsync("Integration Yeni Yazi");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/newsletter");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Integration Yeni Yazi");
        html.Should().Contain("Bekleyen"); // bekleyen makaleler basligi
    }

    [Fact]
    public async Task PostCreateJob_ValidArticle_CreatesPendingJobInDb()
    {
        await ClearArticlesAndJobsAsync();
        var articleId = await SeedPublishedArticleAsync("Pending Job Test");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/newsletter");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await client.PostAsync($"/admin/newsletter/create-job/{articleId}", formData);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db.Set<NewsletterJob>().FirstOrDefaultAsync(j => j.ArticleId == articleId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(NewsletterJobStatus.Pending);
    }

    [Fact]
    public async Task NewsletterJobInsert_DoesNotProduceAuditLog()
    {
        await ClearArticlesAndJobsAsync();
        var articleId = await SeedPublishedArticleAsync("Audit Test");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<AuditLog>().RemoveRange(db.Set<AuditLog>().ToList());
            await db.SaveChangesAsync();

            db.Set<NewsletterJob>().Add(new NewsletterJob
            {
                ArticleId = articleId,
                Status = NewsletterJobStatus.Pending
            });
            await db.SaveChangesAsync();
        }

        using var verify = _factory.Services.CreateScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var newsletterAudits = await db2.Set<AuditLog>()
            .Where(a => a.EntityName == "NewsletterJob")
            .ToListAsync();

        newsletterAudits.Should().BeEmpty("NewsletterJob audit ignore listesinde");
    }

    [Fact]
    public async Task ArticleWithNewsletterSentAtFilled_NotInPendingList()
    {
        // Migration backfill simulasyonu: NewsletterSentAt dolu makaleler pending listesinde gorunmemeli
        await ClearArticlesAndJobsAsync();
        await SeedPublishedArticleAsync("Eski Backfill", newsletterSentAt: DateTime.UtcNow.AddDays(-30));
        await SeedPublishedArticleAsync("Yeni Bekleyen"); // NewsletterSentAt null

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/newsletter");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Yeni Bekleyen");
        html.Should().NotContain("Eski Backfill");
    }

    [Fact]
    public async Task GetPreview_ValidArticle_ReturnsHtmlEmailContent()
    {
        await ClearArticlesAndJobsAsync();
        var articleId = await SeedPublishedArticleAsync("Onizleme Test Yazi");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync($"/admin/newsletter/preview/{articleId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Onizleme Test Yazi");
        html.Should().Contain("Devam"); // "Devamını oku" buton (ASCII-safe)
        html.Should().Contain("/tr-TR/abone/abonelikten-cik/"); // KVKK abonelikten çıkma linki
    }
}

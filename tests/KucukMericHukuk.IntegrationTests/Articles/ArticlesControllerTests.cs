using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Articles;

public class ArticlesControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ArticlesControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/admin/articles");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/account/login");
    }

    [Fact]
    public async Task Index_AuthenticatedAdmin_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/articles");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Makaleler");
    }

    [Fact]
    public async Task Create_FullRoundTrip_PersistsArticleAndTagsAndStatus()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var tagIds = await SeedTagsAsync("itag-a", "itag-b");

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/articles/create");

        var formFields = new List<KeyValuePair<string, string>>
        {
            new("Status", ((int)ArticleStatus.Published).ToString()),
            new("IsFeatured", "false"),
            new("FeaturedImageUrl", ""),
            new("Translations[0].LanguageCode", "tr-TR"),
            new("Translations[0].Title", "Round Trip Makalesi"),
            new("Translations[0].Slug", ""),
            new("Translations[0].Content", "<p>Makale içeriği round-trip testidir.</p>"),
            new("__RequestVerificationToken", token),
        };
        foreach (var tagId in tagIds)
            formFields.Add(new KeyValuePair<string, string>("TagIds", tagId.ToString()));

        var content = new FormUrlEncodedContent(formFields);
        var response = await client.PostAsync("/admin/articles/create", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/articles/details/");

        // Details GET
        var detailsResp = await client.GetAsync(response.Headers.Location);
        detailsResp.IsSuccessStatusCode.Should().BeTrue();
        var html = await detailsResp.Content.ReadAsStringAsync();
        html.Should().Contain("Round Trip Makalesi");
        html.Should().Contain("Yay"); // "Yayında" or "Yayın Tarihi" — ASCII-only substring

        // DB verify: tags attached + slug generated + Published+PublishedAt
        var idStr = response.Headers.Location!.OriginalString.Split('/').Last();
        var id = int.Parse(idStr);

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = await ctx.Set<Article>()
            .Include(a => a.Translations)
            .Include(a => a.Tags)
            .FirstAsync(a => a.Id == id);

        article.Status.Should().Be(ArticleStatus.Published);
        article.PublishedAt.Should().NotBeNull();
        article.Tags.Should().HaveCount(2);
        article.Translations.Should().HaveCount(1);
        article.Translations.First().Slug.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Edit_DraftToPublished_SetsPublishedAt()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var articleId = await SeedDraftArticleAsync("draft-flow-makale", "Taslak Makale");

        var editToken = await TestHelpers.GetAntiForgeryTokenAsync(client, $"/admin/articles/edit/{articleId}");

        var fields = new List<KeyValuePair<string, string>>
        {
            new("Id", articleId.ToString()),
            new("Status", ((int)ArticleStatus.Published).ToString()),
            new("IsFeatured", "false"),
            new("FeaturedImageUrl", ""),
            new("Translations[0].LanguageCode", "tr-TR"),
            new("Translations[0].Title", "Taslak Makale"),
            new("Translations[0].Slug", "draft-flow-makale"),
            new("Translations[0].Content", "<p>İçerik.</p>"),
            new("__RequestVerificationToken", editToken),
        };
        var response = await client.PostAsync($"/admin/articles/edit/{articleId}",
            new FormUrlEncodedContent(fields));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = await ctx.Set<Article>().FirstAsync(a => a.Id == articleId);
        article.Status.Should().Be(ArticleStatus.Published);
        article.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_Restore_Cycle_Works()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var articleId = await SeedDraftArticleAsync("delete-cycle-makale", "Silinecek Makale");

        // Delete
        var delToken = await TestHelpers.GetAntiForgeryTokenAsync(client, $"/admin/articles/details/{articleId}");
        var delResp = await client.PostAsync($"/admin/articles/delete/{articleId}",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", delToken),
            }));
        delResp.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var article = await ctx.Set<Article>().IgnoreQueryFilters().FirstAsync(a => a.Id == articleId);
            article.IsDeleted.Should().BeTrue();
        }

        // IncludeDeleted listesinden restore token al
        var listResp = await client.GetAsync("/admin/articles?includeDeleted=true");
        listResp.IsSuccessStatusCode.Should().BeTrue();
        var listHtml = await listResp.Content.ReadAsStringAsync();
        listHtml.Should().Contain("Silinecek Makale");

        var restoreToken = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/articles?includeDeleted=true");
        var restoreResp = await client.PostAsync($"/admin/articles/restore/{articleId}",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", restoreToken),
            }));
        restoreResp.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var article = await ctx.Set<Article>().FirstAsync(a => a.Id == articleId);
            article.IsDeleted.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Create_DuplicateSlug_AutoSuffixesSecondSlug()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        await SeedDraftArticleAsync("uniq-slug-test", "İlk Makale");

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/articles/create");

        var fields = new List<KeyValuePair<string, string>>
        {
            new("Status", ((int)ArticleStatus.Draft).ToString()),
            new("IsFeatured", "false"),
            new("FeaturedImageUrl", ""),
            new("Translations[0].LanguageCode", "tr-TR"),
            new("Translations[0].Title", "İkinci Başlık"),
            new("Translations[0].Slug", "uniq-slug-test"), // çakışan
            new("Translations[0].Content", "<p>Yeni içerik.</p>"),
            new("__RequestVerificationToken", token),
        };
        var response = await client.PostAsync("/admin/articles/create",
            new FormUrlEncodedContent(fields));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/articles/details/");

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var translations = await ctx.Set<ArticleTranslation>()
            .Where(t => t.Slug.StartsWith("uniq-slug-test"))
            .ToListAsync();
        translations.Should().HaveCount(2);
        translations.Select(t => t.Slug).Should().BeEquivalentTo(new[] { "uniq-slug-test", "uniq-slug-test-2" });
    }

    // === Test fixtures helpers ===

    private async Task<List<int>> SeedTagsAsync(params string[] slugs)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ids = new List<int>();
        foreach (var slug in slugs)
        {
            var existing = await ctx.Set<Tag>()
                .Include(t => t.Translations)
                .FirstOrDefaultAsync(t => t.Translations.Any(tr => tr.Slug == slug));
            if (existing != null)
            {
                ids.Add(existing.Id);
                continue;
            }

            var tag = new Tag
            {
                IsActive = true,
                Translations = new List<TagTranslation>
                {
                    new()
                    {
                        LanguageCode = "tr-TR",
                        Name = slug,
                        Slug = slug,
                        CreatedAt = DateTime.UtcNow,
                    }
                },
                CreatedAt = DateTime.UtcNow,
            };
            ctx.Set<Tag>().Add(tag);
            await ctx.SaveChangesAsync();
            ids.Add(tag.Id);
        }
        return ids;
    }

    private async Task<int> SeedDraftArticleAsync(string slug, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = new Article
        {
            Status = ArticleStatus.Draft,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            Translations = new List<ArticleTranslation>
            {
                new()
                {
                    LanguageCode = "tr-TR",
                    Title = title,
                    Slug = slug,
                    Content = "<p>İçerik.</p>",
                    ReadingTimeMinutes = 1,
                    CreatedAt = DateTime.UtcNow,
                }
            }
        };
        ctx.Set<Article>().Add(article);
        await ctx.SaveChangesAsync();
        return article.Id;
    }
}

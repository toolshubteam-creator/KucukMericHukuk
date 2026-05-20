using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Articles;

public class ArticleEditorIdTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ArticleEditorIdTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EditAsAdmin_SetsEditorId_PersistsToDb()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var editorId = await GetUserIdAsync(IntegrationTestFactory.EditorEmail);
        var articleId = await SeedArticleAsync("admin-sets-editor", "Admin Editor Atayacak", authorId, editorId: null);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AdminEmail, IntegrationTestFactory.AdminPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, $"/admin/articles/edit/{articleId}");
        var form = BuildEditForm(token, articleId, authorId: authorId, editorId: editorId,
            title: "Admin Editor Atayacak (guncel)");

        var response = await client.PostAsync($"/admin/articles/edit/{articleId}",
            new FormUrlEncodedContent(form));

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await ctx.Set<Article>().AsNoTracking().FirstAsync(a => a.Id == articleId);
        stored.EditorId.Should().Be(editorId);
    }

    [Fact]
    public async Task EditAsEditor_CanChangeEditorId()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var editorId = await GetUserIdAsync(IntegrationTestFactory.EditorEmail);
        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);
        var articleId = await SeedArticleAsync("editor-changes-editor", "Editor Editor Degistirir",
            authorId, editorId: null);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, $"/admin/articles/edit/{articleId}");
        var form = BuildEditForm(token, articleId, authorId: authorId, editorId: editorId,
            title: "Editor Editor Degistirir (guncel)");

        var response = await client.PostAsync($"/admin/articles/edit/{articleId}",
            new FormUrlEncodedContent(form));

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await ctx.Set<Article>().AsNoTracking().FirstAsync(a => a.Id == articleId);
        stored.EditorId.Should().Be(editorId);
    }

    [Fact]
    public async Task EditAsAuthor_EditorIdHijackPrevented()
    {
        // Author rolü form'da farklı bir EditorId gönderse de DB'de eski değer kalmalı.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var editorId = await GetUserIdAsync(IntegrationTestFactory.EditorEmail);
        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);

        // Makaleyi editorId=editor olarak başlat
        var articleId = await SeedArticleAsync("author-hijack-editor", "Author Editor Hijack",
            authorId, editorId: editorId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, $"/admin/articles/edit/{articleId}");
        var form = BuildEditForm(token, articleId,
            authorId: authorId,
            editorId: adminId, // hijack denemesi (Admin'e atamaya çalış)
            title: "Author Editor Hijack (guncel)");

        await client.PostAsync($"/admin/articles/edit/{articleId}",
            new FormUrlEncodedContent(form));

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await ctx.Set<Article>().AsNoTracking().FirstAsync(a => a.Id == articleId);
        stored.EditorId.Should().Be(editorId, "Author hijack denemesi silent override edilmeli (eski editor korunur)");
    }

    [Fact]
    public async Task PublicDetail_RendersEditorName_WhenSet()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var editorId = await GetUserIdAsync(IntegrationTestFactory.EditorEmail);

        var articleId = await SeedArticleAsync("public-with-editor", "Editorlu Public Makale",
            authorId, editorId: editorId);
        await PublishAsync(articleId);

        var response = await client.GetAsync("/tr-TR/makaleler/public-with-editor");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Test Editor", "Editor FullName public makale meta'da render edilmeli");
        html.Should().Contain("Edit", "'Editör:' label render edilmeli (Edit ASCII alt-string)");
    }

    [Fact]
    public async Task PublicDetail_OmitsEditor_WhenNotSet()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);

        var articleId = await SeedArticleAsync("public-no-editor", "Editorsuz Public Makale",
            authorId, editorId: null);
        await PublishAsync(articleId);

        var response = await client.GetAsync("/tr-TR/makaleler/public-no-editor");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        // Editor atanmadıysa Test Editor adı public sayfada çıkmamalı
        html.Should().NotContain("Test Editor",
            "EditorId boşken Editor adı public makalede render edilmemeli");
    }

    [Fact]
    public async Task PublicDetail_JsonLd_IncludesEditor_WhenSet()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var editorId = await GetUserIdAsync(IntegrationTestFactory.EditorEmail);

        var articleId = await SeedArticleAsync("jsonld-with-editor", "JSON-LD Editor Test",
            authorId, editorId: editorId);
        await PublishAsync(articleId);

        var response = await client.GetAsync("/tr-TR/makaleler/jsonld-with-editor");
        var html = await response.Content.ReadAsStringAsync();

        // JSON-LD schema'da "editor": { ... "name": "Test Editor" }
        html.Should().Contain("\"editor\"", "JSON-LD schema'da editor field olmalı");
        html.Should().Contain("\"name\":\"Test Editor\"",
            "JSON-LD editor.name Test Editor olmalı");
    }

    [Fact]
    public async Task PublicDetail_JsonLd_OmitsEditor_WhenNotSet()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);

        var articleId = await SeedArticleAsync("jsonld-no-editor", "JSON-LD Editorsuz",
            authorId, editorId: null);
        await PublishAsync(articleId);

        var response = await client.GetAsync("/tr-TR/makaleler/jsonld-no-editor");
        var html = await response.Content.ReadAsStringAsync();

        html.Should().NotContain("\"editor\":",
            "EditorId yokken JSON-LD'de editor field render edilmemeli (Rich Results validation)");
    }

    // === Helpers ===

    private async Task<int> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"Test fixture user not seeded: {email}");
        return user.Id;
    }

    private async Task<int> SeedArticleAsync(string slug, string title, int? authorId, int? editorId)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = new Article
        {
            AuthorId = authorId,
            EditorId = editorId,
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
                    Content = "<p>Test icerik.</p>",
                    ReadingTimeMinutes = 1,
                    CreatedAt = DateTime.UtcNow,
                }
            }
        };
        ctx.Set<Article>().Add(article);
        await ctx.SaveChangesAsync();
        return article.Id;
    }

    private async Task PublishAsync(int articleId)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = await ctx.Set<Article>().FirstAsync(a => a.Id == articleId);
        article.Status = ArticleStatus.Published;
        article.PublishedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    private static List<KeyValuePair<string, string>> BuildEditForm(
        string token, int articleId, int? authorId, int? editorId, string title)
    {
        return new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("Id", articleId.ToString()),
            new("AuthorId", authorId?.ToString() ?? string.Empty),
            new("EditorId", editorId?.ToString() ?? string.Empty),
            new("Status", ((int)ArticleStatus.Draft).ToString()),
            new("IsFeatured", "false"),
            new("Translations[0].LanguageCode", "tr-TR"),
            new("Translations[0].Title", title),
            new("Translations[0].Content", "<p>guncel.</p>"),
        };
    }
}

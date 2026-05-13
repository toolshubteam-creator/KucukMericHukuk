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

public class ArticlesEditorAuthzTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ArticlesEditorAuthzTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // AUTHOR ROLÜ — sadece kendi makalelerini görür ve düzenler
    // ============================================================

    [Fact]
    public async Task Index_AuthorAccess_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var response = await client.GetAsync("/admin/articles");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Makaleler");
    }

    [Fact]
    public async Task Index_Author_ShowsOnlyOwnArticles()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);

        await SeedArticleAsync("author-own-makale", "Author Kendi Makalesi", authorId);
        await SeedArticleAsync("admin-only-makale", "Admin Yalniz Makale", adminId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var response = await client.GetAsync("/admin/articles");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Author Kendi Makalesi");
        html.Should().NotContain("Admin Yalniz Makale");
    }

    [Fact]
    public async Task Edit_AuthorOnOthersArticle_ReturnsForbid()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);
        var adminArticleId = await SeedArticleAsync("admin-edit-target", "Admin Edit Hedefi", adminId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var response = await client.GetAsync($"/admin/articles/edit/{adminArticleId}");

        // Cookie auth Forbid() → 302 AccessDenied
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location?.OriginalString.Should().Contain("access-denied");
    }

    [Fact]
    public async Task Delete_AuthorOnOthersArticle_ReturnsForbid()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);
        var adminArticleId = await SeedArticleAsync("admin-delete-target", "Admin Delete Hedefi", adminId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/articles");
        var response = await client.PostAsync($"/admin/articles/delete/{adminArticleId}",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            }));

        // Cookie auth Forbid() → 302 AccessDenied
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location?.OriginalString.Should().Contain("access-denied");
    }

    [Fact]
    public async Task Edit_AuthorOnOwnArticle_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var ownArticleId = await SeedArticleAsync("author-own-edit", "Author Kendi Edit", authorId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var response = await client.GetAsync($"/admin/articles/edit/{ownArticleId}");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task AdminOnlyController_AuthorBypass_ReturnsForbid()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        // Cookie auth Forbid() → 302 AccessDenied
        var pagesResp = await client.GetAsync("/admin/pages");
        pagesResp.StatusCode.Should().Be(HttpStatusCode.Found);
        pagesResp.Headers.Location?.OriginalString.Should().Contain("access-denied");

        var usersResp = await client.GetAsync("/admin/users");
        usersResp.StatusCode.Should().Be(HttpStatusCode.Found);
        usersResp.Headers.Location?.OriginalString.Should().Contain("access-denied");

        var siteSettingsResp = await client.GetAsync("/admin/site-settings");
        siteSettingsResp.StatusCode.Should().Be(HttpStatusCode.Found);
        siteSettingsResp.Headers.Location?.OriginalString.Should().Contain("access-denied");
    }

    [Fact]
    public async Task Sidebar_Author_HidesAdminOnlyLinks()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var response = await client.GetAsync("/admin");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("Makaleler");
        html.Should().Contain("Kontrol Paneli");
        html.Should().NotContain("/admin/pages");
        html.Should().NotContain("/admin/services");
        html.Should().NotContain("/admin/users");
        html.Should().NotContain("/admin/site-settings");
    }

    [Fact]
    public async Task EditPost_Author_CannotHijackAuthorId()
    {
        // Author rolündeki kullanıcı, form'da farklı bir AuthorId göndererek
        // başka kullanıcıya makale atayamamalıdır — controller AuthorId'yi
        // session user'a sabitlemeli (silent override).
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);
        var ownArticleId = await SeedArticleAsync("author-hijack-test", "Hijack Test", authorId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AuthorEmail, IntegrationTestFactory.AuthorPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, $"/admin/articles/edit/{ownArticleId}");
        var form = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("Id", ownArticleId.ToString()),
            new("AuthorId", adminId.ToString()), // hijack denemesi
            new("Status", ((int)ArticleStatus.Draft).ToString()),
            new("IsFeatured", "false"),
            new("Translations[0].LanguageCode", "tr-TR"),
            new("Translations[0].Title", "Hijack Test (guncel)"),
            new("Translations[0].Content", "<p>guncel.</p>"),
        };

        var response = await client.PostAsync($"/admin/articles/edit/{ownArticleId}",
            new FormUrlEncodedContent(form));

        // Redirect (success) ya da 200 (validation hata göstergesi) — her halükarda
        // AuthorId DB'de hâlâ author'a ait olmalı.
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await ctx.Set<Article>().AsNoTracking()
            .FirstAsync(a => a.Id == ownArticleId);
        stored.AuthorId.Should().Be(authorId, "Author hijack denemesi silent override edilmeli");
    }

    // ============================================================
    // EDITOR ROLÜ — tüm makaleleri görür ve düzenler
    // ============================================================

    [Fact]
    public async Task Index_EditorAccess_ShowsAllArticles()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);

        await SeedArticleAsync("editor-sees-author", "Editor Author Makalesi Gorur", authorId);
        await SeedArticleAsync("editor-sees-admin", "Editor Admin Makalesi Gorur", adminId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var response = await client.GetAsync("/admin/articles");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Editor Author Makalesi Gorur");
        html.Should().Contain("Editor Admin Makalesi Gorur");
    }

    [Fact]
    public async Task Edit_EditorOnAnyArticle_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var authorId = await GetUserIdAsync(IntegrationTestFactory.AuthorEmail);
        var authorArticleId = await SeedArticleAsync("editor-edit-author-article",
            "Editor Author Makalesi Duzenler", authorId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var response = await client.GetAsync($"/admin/articles/edit/{authorArticleId}");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task AdminOnlyController_EditorBypass_ReturnsForbid()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        // Editor sadece Articles ve Dashboard. Admin-only controller'lara erişim yok.
        var pagesResp = await client.GetAsync("/admin/pages");
        pagesResp.StatusCode.Should().Be(HttpStatusCode.Found);
        pagesResp.Headers.Location?.OriginalString.Should().Contain("access-denied");

        var usersResp = await client.GetAsync("/admin/users");
        usersResp.StatusCode.Should().Be(HttpStatusCode.Found);
        usersResp.Headers.Location?.OriginalString.Should().Contain("access-denied");

        var siteSettingsResp = await client.GetAsync("/admin/site-settings");
        siteSettingsResp.StatusCode.Should().Be(HttpStatusCode.Found);
        siteSettingsResp.Headers.Location?.OriginalString.Should().Contain("access-denied");
    }

    [Fact]
    public async Task Sidebar_Editor_ShowsArticlesHidesAdminOnly()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var response = await client.GetAsync("/admin");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("Makaleler");
        html.Should().Contain("Kontrol Paneli");
        html.Should().NotContain("/admin/pages");
        html.Should().NotContain("/admin/services");
        html.Should().NotContain("/admin/users");
        html.Should().NotContain("/admin/site-settings");
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

    private async Task<int> SeedArticleAsync(string slug, string title, int? authorId, int? editorId = null)
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
}

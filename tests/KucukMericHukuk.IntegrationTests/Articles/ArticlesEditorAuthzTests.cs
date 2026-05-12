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

    [Fact]
    public async Task Index_EditorAccess_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var response = await client.GetAsync("/admin/articles");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Makaleler");
    }

    [Fact]
    public async Task Index_Editor_ShowsOnlyOwnArticles()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var editorId = await GetUserIdAsync(IntegrationTestFactory.EditorEmail);
        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);

        await SeedArticleAsync("editor-own-makale", "Editor Kendi Makalesi", editorId);
        await SeedArticleAsync("admin-only-makale", "Admin Yalniz Makale", adminId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var response = await client.GetAsync("/admin/articles");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Editor Kendi Makalesi");
        html.Should().NotContain("Admin Yalniz Makale");
    }

    [Fact]
    public async Task Edit_EditorOnOthersArticle_ReturnsForbid()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);
        var adminArticleId = await SeedArticleAsync("admin-edit-target", "Admin Edit Hedefi", adminId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var response = await client.GetAsync($"/admin/articles/edit/{adminArticleId}");

        // Cookie auth Forbid() → 302 AccessDenied
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location?.OriginalString.Should().Contain("access-denied");
    }

    [Fact]
    public async Task Delete_EditorOnOthersArticle_ReturnsForbid()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var adminId = await GetUserIdAsync(IntegrationTestFactory.AdminEmail);
        var adminArticleId = await SeedArticleAsync("admin-delete-target", "Admin Delete Hedefi", adminId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

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
    public async Task Edit_EditorOnOwnArticle_Returns200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var editorId = await GetUserIdAsync(IntegrationTestFactory.EditorEmail);
        var ownArticleId = await SeedArticleAsync("editor-own-edit", "Editor Kendi Edit", editorId);

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.EditorEmail, IntegrationTestFactory.EditorPassword);

        var response = await client.GetAsync($"/admin/articles/edit/{ownArticleId}");

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
    public async Task Sidebar_Editor_HidesAdminOnlyLinks()
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

    private async Task<int> SeedArticleAsync(string slug, string title, int? authorId)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = new Article
        {
            AuthorId = authorId,
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

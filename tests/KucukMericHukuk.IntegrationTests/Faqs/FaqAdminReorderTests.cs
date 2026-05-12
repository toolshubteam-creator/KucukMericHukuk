using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Faqs;

/// <summary>
/// Faz 6.6b — Admin Faq drag-drop reorder endpoint integration tests.
/// POST /admin/faqs/reorder JSON body + RequestVerificationToken header pattern.
/// </summary>
public class FaqAdminReorderTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public FaqAdminReorderTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int id1, int id2, int id3)> SeedThreeFaqsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<Faq>().RemoveRange(db.Set<Faq>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();

        var faqs = new[]
        {
            new Faq { DisplayOrder = 0, IsActive = true, Translations = new List<FaqTranslation> { new() { LanguageCode = "tr-TR", Question = "Q1", Answer = "A1" } } },
            new Faq { DisplayOrder = 1, IsActive = true, Translations = new List<FaqTranslation> { new() { LanguageCode = "tr-TR", Question = "Q2", Answer = "A2" } } },
            new Faq { DisplayOrder = 2, IsActive = true, Translations = new List<FaqTranslation> { new() { LanguageCode = "tr-TR", Question = "Q3", Answer = "A3" } } },
        };
        db.Set<Faq>().AddRange(faqs);
        await db.SaveChangesAsync();

        return (faqs[0].Id, faqs[1].Id, faqs[2].Id);
    }

    private async Task<int> ReadDisplayOrderAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var faq = await db.Set<Faq>().FirstAsync(f => f.Id == id);
        return faq.DisplayOrder;
    }

    [Fact]
    public async Task PostReorder_Authorized_Returns200_PersistsNewOrder()
    {
        var (id1, id2, id3) = await SeedThreeFaqsAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        // GET Index'ten anti-forgery token al
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/faqs");

        // Tersine: id3 -> 0, id2 -> 1, id1 -> 2
        var payload = new[]
        {
            new { Id = id3, DisplayOrder = 0 },
            new { Id = id2, DisplayOrder = 1 },
            new { Id = id1, DisplayOrder = 2 },
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/faqs/reorder")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("RequestVerificationToken", token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReadDisplayOrderAsync(id3)).Should().Be(0);
        (await ReadDisplayOrderAsync(id2)).Should().Be(1);
        (await ReadDisplayOrderAsync(id1)).Should().Be(2);
    }

    [Fact]
    public async Task PostReorder_Anonymous_RedirectsToLogin()
    {
        await SeedThreeFaqsAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/faqs/reorder")
        {
            Content = JsonContent.Create(Array.Empty<object>())
        };

        var response = await client.SendAsync(request);

        // Authorize attribute admin login'e redirect eder (302) — JSON 401 değil, mevcut admin auth pattern
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/account/login");
    }

    [Fact]
    public async Task PostReorder_IdSetMismatch_Returns400()
    {
        var (id1, _, _) = await SeedThreeFaqsAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/faqs");

        // Sadece 1 ID gönder — DB'de 3 var, set mismatch
        var payload = new[]
        {
            new { Id = id1, DisplayOrder = 0 },
            new { Id = 99999, DisplayOrder = 1 }, // DB'de yok
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/faqs/reorder")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("RequestVerificationToken", token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

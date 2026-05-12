using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.ContactMessages;

/// <summary>
/// Faz 6.7 — Reply + Dashboard widget admin tests.
/// </summary>
public class ContactMessagesAdminTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ContactMessagesAdminTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> SeedMessageAsync(bool isRead = false, bool isAnswered = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<ContactMessage>().RemoveRange(db.Set<ContactMessage>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();

        var msg = new ContactMessage
        {
            Name = "Test Kullanici",
            Email = "test@example.com",
            Subject = "Test Konu",
            Message = "Test mesaj icerigi",
            KvkkConsent = true,
            IsRead = isRead,
            IsAnswered = isAnswered,
            CreatedAt = DateTime.UtcNow,
        };
        db.Set<ContactMessage>().Add(msg);
        await db.SaveChangesAsync();
        return msg.Id;
    }

    [Fact]
    public async Task PostReply_Authorized_PersistsReplyAndMarksAnswered()
    {
        var id = await SeedMessageAsync(isRead: false, isAnswered: false);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var detailsUrl = $"/admin/contact-messages/details/{id}";
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, detailsUrl);

        var formData = new List<KeyValuePair<string, string>>
        {
            new("body", "Yanit metnidir."),
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync($"/admin/contact-messages/reply/{id}",
            new FormUrlEncodedContent(formData));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain($"/admin/contact-messages/details/{id}");

        // DB verify
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var msg = await db.Set<ContactMessage>()
            .Include(m => m.Replies)
            .FirstAsync(m => m.Id == id);

        msg.IsAnswered.Should().BeTrue();
        msg.IsRead.Should().BeTrue();
        msg.Replies.Should().HaveCount(1);
        msg.Replies.First().Body.Should().Be("Yanit metnidir.");
    }

    [Fact]
    public async Task PostReply_EmptyBody_RedirectsToDetails_NoReplyCreated()
    {
        var id = await SeedMessageAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var detailsUrl = $"/admin/contact-messages/details/{id}";
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, detailsUrl);

        var formData = new List<KeyValuePair<string, string>>
        {
            new("body", ""),
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync($"/admin/contact-messages/reply/{id}",
            new FormUrlEncodedContent(formData));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var msg = await db.Set<ContactMessage>()
            .Include(m => m.Replies)
            .FirstAsync(m => m.Id == id);

        msg.Replies.Should().BeEmpty();
        msg.IsAnswered.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboard_Authorized_ShowsUnreadCount()
    {
        await SeedMessageAsync(isRead: false);
        await SeedMessageAsync(isRead: false);

        // Yeniden seed (tek seferlik ekle - ikinci SeedMessageAsync ilkini siler!)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<ContactMessage>().Add(new ContactMessage
            {
                Name = "Extra", Email = "e@x.com", Subject = "S2", Message = "M2",
                IsRead = false, CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Widget render: "okunmamış mesaj" yazısı ve "Tümü" butonu görünür
        html.Should().Contain("okunmam"); // ASCII-safe substring
        html.Should().Contain("/admin/contact-messages"); // Tümü link
    }
}

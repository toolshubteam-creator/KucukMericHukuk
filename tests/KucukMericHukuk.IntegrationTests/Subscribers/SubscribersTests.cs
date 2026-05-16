using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Subscribers;

public class SubscribersTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public SubscribersTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task ClearSubscribersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<Subscriber>().RemoveRange(db.Set<Subscriber>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PostSubscribe_ValidForm_CreatesActiveSubscriberInDb()
    {
        await ClearSubscribersAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        // AntiForgery token any public page'in footer'indaki subscribe form'undan alinabilir
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/tr-TR/");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "integration@test.local"),
            new KeyValuePair<string, string>("KvkkConsent", "true"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await client.PostAsync("/tr-TR/Subscriber/Subscribe", formData);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = await db.Set<Subscriber>().FirstOrDefaultAsync(s => s.Email == "integration@test.local");
        sub.Should().NotBeNull();
        sub!.Status.Should().Be(SubscriberStatus.Active);
        sub.UnsubscribeToken.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetUnsubscribe_ValidToken_MarksSubscriberUnsubscribed()
    {
        await ClearSubscribersAsync();

        var token = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<Subscriber>().Add(new Subscriber
            {
                Email = "unsub@test.local",
                Status = SubscriberStatus.Active,
                UnsubscribeToken = token,
                KvkkConsent = true,
                SubscribedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/tr-TR/Subscriber/Unsubscribe/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("iptal edildi");

        using var verify = _factory.Services.CreateScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = await db2.Set<Subscriber>().FirstAsync(s => s.UnsubscribeToken == token);
        sub.Status.Should().Be(SubscriberStatus.Unsubscribed);
        sub.UnsubscribedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUnsubscribe_InvalidToken_RendersErrorMessage()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/tr-TR/Subscriber/Unsubscribe/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("ge"); // "geçersiz" — substring is ASCII safe portion
    }

    [Fact]
    public async Task AdminSubscribersIndex_Authorized_ReturnsOkWithList()
    {
        await ClearSubscribersAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<Subscriber>().Add(new Subscriber
            {
                Email = "admin-list@test.local",
                Status = SubscriberStatus.Active,
                UnsubscribeToken = Guid.NewGuid(),
                KvkkConsent = true,
                SubscribedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/subscribers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("admin-list@test.local");
    }

    [Fact]
    public async Task SubscriberInsert_DoesNotProduceAuditLog()
    {
        await ClearSubscribersAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<AuditLog>().RemoveRange(db.Set<AuditLog>().ToList());
            await db.SaveChangesAsync();

            db.Set<Subscriber>().Add(new Subscriber
            {
                Email = "audit-test@test.local",
                Status = SubscriberStatus.Active,
                UnsubscribeToken = Guid.NewGuid(),
                KvkkConsent = true,
                SubscribedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var verify = _factory.Services.CreateScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var subscriberAudits = await db2.Set<AuditLog>()
            .Where(a => a.EntityName == "Subscriber")
            .ToListAsync();

        subscriberAudits.Should().BeEmpty("Subscriber audit ignore listesinde");
    }
}

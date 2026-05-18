using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.NotFoundLogs;

/// <summary>
/// Faz 7.4.3b — 404 → Redirect Kur köprü akışı (7.3↔7.4 birleşmesi).
/// </summary>
public class NotFoundRedirectBridgeTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public NotFoundRedirectBridgeTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task ResetTablesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.NotFoundLogs.RemoveRange(db.NotFoundLogs.ToList());
        db.Redirects.RemoveRange(db.Redirects.ToList());
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedNotFoundLogAsync(string url)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var n = new NotFoundLog
        {
            Id = Guid.NewGuid(),
            Url = url,
            HitCount = 1,
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        db.NotFoundLogs.Add(n);
        await db.SaveChangesAsync();
        return n.Id;
    }

    private async Task SeedRedirectAsync(string from, string to)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Redirects.Add(new Redirect
        {
            FromPath = from,
            ToPath = to,
            StatusCode = 301,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var login = await TestHelpers.LoginAsync(client);
        login.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.Redirect, System.Net.HttpStatusCode.OK);
        return client;
    }

    private static async Task<HttpResponseMessage> PostCreateRedirectAsync(
        HttpClient client, Guid notFoundId, string toPath, int statusCode = 301)
    {
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/notfound-logs");
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("toPath", toPath),
            new KeyValuePair<string, string>("statusCode", statusCode.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });
        return await client.PostAsync($"/admin/notfound-logs/create-redirect/{notFoundId}", content);
    }

    [Fact]
    public async Task CreateRedirect_Valid_CreatesRedirectAndPurgesNotFoundLog()
    {
        await ResetTablesAsync();
        var notFoundId = await SeedNotFoundLogAsync("/tr-TR/eski-404");

        var client = await CreateAdminClientAsync();
        var response = await PostCreateRedirectAsync(client, notFoundId, "/tr-TR/yeni-hedef");

        // PRG: 302/303 redirect → Index
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Redirect,
            System.Net.HttpStatusCode.SeeOther);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var redirect = await db.Redirects.AsNoTracking()
            .FirstOrDefaultAsync(r => r.FromPath == "/tr-TR/eski-404");
        redirect.Should().NotBeNull("kopru akisi RedirectService.CreateAsync cagirmali");
        redirect!.ToPath.Should().Be("/tr-TR/yeni-hedef");
        redirect.StatusCode.Should().Be(301);
        redirect.IsActive.Should().BeTrue();

        var notFound = await db.NotFoundLogs.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notFoundId);
        notFound.Should().BeNull("redirect basarili oldugunda 404 kaydi cozuldu sayilip silinmeli");
    }

    [Fact]
    public async Task CreateRedirect_DuplicateFromPath_RejectedAndNotFoundLogPreserved()
    {
        await ResetTablesAsync();
        var notFoundId = await SeedNotFoundLogAsync("/tr-TR/duplicate-test");
        // FromPath icin zaten manuel redirect var
        await SeedRedirectAsync("/tr-TR/duplicate-test", "/tr-TR/manuel-hedef");

        var client = await CreateAdminClientAsync();
        var response = await PostCreateRedirectAsync(client, notFoundId, "/tr-TR/yeni-istek");

        // Yine PRG redirect (Index'e) — hata TempData ile gosterilir
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Redirect,
            System.Net.HttpStatusCode.SeeOther);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Yeni redirect olusmamali (duplicate red)
        var redirects = await db.Redirects.AsNoTracking()
            .Where(r => r.FromPath == "/tr-TR/duplicate-test")
            .ToListAsync();
        redirects.Should().HaveCount(1, "duplicate reddi - sadece mevcut manuel redirect var");
        redirects[0].ToPath.Should().Be("/tr-TR/manuel-hedef");

        // NotFoundLog korunmali (admin tekrar deneyebilsin)
        var notFound = await db.NotFoundLogs.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notFoundId);
        notFound.Should().NotBeNull("redirect basarisiz oldugunda 404 kaydi SILINMEMELI");
    }

    [Fact]
    public async Task CreateRedirect_SelfRedirect_RejectedAndNotFoundLogPreserved()
    {
        await ResetTablesAsync();
        var notFoundId = await SeedNotFoundLogAsync("/tr-TR/kendine-yonlendir");

        var client = await CreateAdminClientAsync();
        var response = await PostCreateRedirectAsync(client, notFoundId, "/tr-TR/kendine-yonlendir");

        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Redirect,
            System.Net.HttpStatusCode.SeeOther);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var redirects = await db.Redirects.AsNoTracking().ToListAsync();
        redirects.Should().BeEmpty("self-redirect reddedilir, satir olusmaz");

        var notFound = await db.NotFoundLogs.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notFoundId);
        notFound.Should().NotBeNull("self-redirect red - 404 kaydi korunur");
    }
}

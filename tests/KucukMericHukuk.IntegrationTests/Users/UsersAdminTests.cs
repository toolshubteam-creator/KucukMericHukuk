using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Users;

/// <summary>
/// Faz 6.8-fix — ConfirmPassword view-level validation + admin Create/ResetPassword flow.
/// Logout endpoint zaten mevcut (Faz 2'den) — bu tests'in kapsamı dışında.
/// </summary>
public class UsersAdminTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public UsersAdminTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCreate_PasswordMismatch_ReturnsViewWithError_NoUserCreated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/users/create");

        var formData = new List<KeyValuePair<string, string>>
        {
            new("UserName", "mismatchuser"),
            new("Email", "mismatch@test.com"),
            new("FullName", "Mismatch User"),
            new("Password", "Pass1234!"),
            new("ConfirmPassword", "WrongPass!"), // mismatch
            new("Role", "Editor"),
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync("/admin/users/create", new FormUrlEncodedContent(formData));

        // Redirect olmamalı (validation fail) — view ile dönmeli
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "ConfirmPassword mismatch ModelState'i invalid yapar, controller View(form) döner");

        // DB'de yeni user oluşmamış olmalı
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("mismatchuser");
        user.Should().BeNull(because: "ModelState invalid olduğu için service.CreateAsync çağrılmamalı");
    }

    [Fact]
    public async Task PostCreate_PasswordMatch_RedirectsToDetails_UserCreated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/users/create");

        var formData = new List<KeyValuePair<string, string>>
        {
            new("UserName", "matchuser"),
            new("Email", "match@test.com"),
            new("FullName", "Match User"),
            new("Password", "Pass1234!"),
            new("ConfirmPassword", "Pass1234!"), // match
            new("Role", "Editor"),
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync("/admin/users/create", new FormUrlEncodedContent(formData));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            because: "Geçerli form -> Details'a redirect");

        // DB'de user oluştu
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("matchuser");
        user.Should().NotBeNull();
        user!.Email.Should().Be("match@test.com");
    }
}

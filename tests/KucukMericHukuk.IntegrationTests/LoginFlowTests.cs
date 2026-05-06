using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KucukMericHukuk.IntegrationTests;

public class LoginFlowTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public LoginFlowTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToAdmin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await TestHelpers.LoginAsync(client);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsLoginPage()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.LoginAsync(client, password: "WrongPassword!");

        // POST sonucu 200 (login sayfası tekrar render) — redirect yok
        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        // AccountController.Login: "E-posta veya şifre hatalı."
        // (Türkçe karakter HTML-encoded olur: "hatal&#x131;")
        html.Should().Contain("E-posta veya");
    }

    [Fact]
    public async Task AfterLogin_AdminPage_Accessible()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/pages");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Sayfalar");
    }
}

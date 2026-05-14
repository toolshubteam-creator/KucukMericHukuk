using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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

/// <summary>
/// Faz 6.19: Turnstile'ı yeniden etkinleştiren factory varyantı. Base factory
/// Turnstile:Enabled=false set eder; bu varyant sonraki in-memory config source ile
/// true'ya çeker. Token boş gönderildiğinde VerifyAsync HTTP çağrısı yapmadan false döner —
/// bu yüzden gerçek Cloudflare bağlantısı gerekmez.
/// </summary>
public class TurnstileEnabledIntegrationTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Turnstile:Enabled"] = "true"
            });
        });
    }
}

public class LoginTurnstileTests : IClassFixture<TurnstileEnabledIntegrationTestFactory>
{
    private readonly TurnstileEnabledIntegrationTestFactory _factory;

    public LoginTurnstileTests(TurnstileEnabledIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithoutTurnstileToken_ReturnsLoginPage_WithoutAuthenticating()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // TestHelpers.LoginAsync TurnstileToken göndermez — Turnstile enabled iken
        // VerifyAsync boş token'ı anında reddeder (HTTP çağrısı yok).
        var response = await TestHelpers.LoginAsync(client);

        // Geçerli kimlik bilgilerine rağmen redirect YOK — Turnstile guard SignInManager'a
        // gidilmeden döndürdü. 200 = login sayfası tekrar render.
        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        // "Bot doğrulaması başarısız oldu." — ASCII-only substring
        html.Should().Contain("Bot do");
        // Lockout mesajı görünmemeli — guard kimlik akışına hiç girmedi
        html.Should().NotContain("kilitlen");
    }
}

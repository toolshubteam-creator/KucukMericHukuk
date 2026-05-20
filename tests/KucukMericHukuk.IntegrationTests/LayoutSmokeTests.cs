using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.RegularExpressions;

namespace KucukMericHukuk.IntegrationTests;

public class LayoutSmokeTests : IClassFixture<TurnstileEnabledIntegrationTestFactory>
{
    private readonly TurnstileEnabledIntegrationTestFactory _turnstileFactory;

    public LayoutSmokeTests(TurnstileEnabledIntegrationTestFactory turnstileFactory)
    {
        _turnstileFactory = turnstileFactory;
    }

    [Fact]
    public async Task PublicLayout_WithTurnstileEnabled_RendersSingleTurnstileLoader()
    {
        var client = _turnstileFactory.CreateClient();

        // /tr-TR/Contact: bu branch'te canonical route; route Türkçeleştirme
        // sonrası 301 ile /tr-TR/iletisim'e yönlenir, client redirect'i takip eder.
        var html = await client.GetStringAsync("/tr-TR/Contact");

        html.Should().Contain("cf-turnstile");
        Regex.Matches(html, "https://challenges.cloudflare.com/turnstile/v0/api.js")
            .Count.Should().Be(1);
    }
}

public class AdminLayoutSmokeTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AdminLayoutSmokeTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminLayout_AfterLogin_RendersTopbarDropdownFix()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var html = await client.GetStringAsync("/admin");

        html.Should().Contain("data-admin-user-menu-toggle");
        html.Should().Contain("/js/admin/admin-topbar.js");
    }
}

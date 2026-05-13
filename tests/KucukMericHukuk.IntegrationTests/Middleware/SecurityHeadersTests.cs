using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;

namespace KucukMericHukuk.IntegrationTests.Middleware;

public class SecurityHeadersTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public SecurityHeadersTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Public_response_includes_x_frame_options_header()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact]
    public async Task Public_response_includes_csp_with_required_directives()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/");
        var csp = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
        csp.Should().NotBeNullOrEmpty();
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("https://challenges.cloudflare.com");
    }

    [Fact]
    public async Task Csp_AllowList_ExcludesCdnSources_AfterSelfHost()
    {
        // Faz 6.17: tüm vendor JS/CSS wwwroot/lib/ altına self-host edildi.
        // CSP'den cdn.jsdelivr.net + unpkg.com tasfiye edildi; geriye sadece Cloudflare Turnstile kaldı.
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/");
        var csp = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();

        csp.Should().NotBeNullOrEmpty();
        csp.Should().NotContain("cdn.jsdelivr.net");
        csp.Should().NotContain("unpkg.com");
        // Turnstile istisna — script-src + frame-src + connect-src'de korunur (Cloudflare-managed)
        csp.Should().MatchRegex(@"script-src[^;]*https://challenges\.cloudflare\.com");
    }
}

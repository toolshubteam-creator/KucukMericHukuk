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
    public async Task Csp_ConnectSrc_IncludesJsDelivr_ForSourceMapFetches()
    {
        // Faz 6.8 fix: cdn.jsdelivr.net connect-src'de olmazsa Bootstrap/Tabler script source map fetch'leri bloklanır
        // → bazı JS özellikleri (dropdown init dahil) hatalı çalışabilir.
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/");
        var csp = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();

        csp.Should().NotBeNullOrEmpty();
        // connect-src direktifinde jsdelivr olmalı
        csp.Should().MatchRegex(@"connect-src[^;]*https://cdn\.jsdelivr\.net");
    }
}

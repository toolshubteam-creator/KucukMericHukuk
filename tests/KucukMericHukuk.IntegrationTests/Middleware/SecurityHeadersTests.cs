using System.Text.RegularExpressions;
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

    [Fact]
    public async Task Csp_ScriptSrc_UsesNonce_NotUnsafeInline()
    {
        // Faz 6.20: script-src'den 'unsafe-inline' kaldırıldı, per-request nonce eklendi.
        // Nonce dinamik olduğu için tam-string eşleşme yerine pattern-based assertion.
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/");
        var csp = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();

        csp.Should().NotBeNullOrEmpty();

        var scriptSrc = ExtractDirective(csp!, "script-src");
        scriptSrc.Should().NotBeNullOrEmpty();
        // script-src nonce içermeli (base64)
        scriptSrc.Should().MatchRegex(@"'nonce-[A-Za-z0-9+/=]+'");
        // script-src 'unsafe-inline' İÇERMEMELİ — hardening'in özü
        scriptSrc.Should().NotContain("'unsafe-inline'");
        // Turnstile host allowlist'i korunmalı
        scriptSrc.Should().Contain("https://challenges.cloudflare.com");

        // style-src 'unsafe-inline' DEĞİŞMEDEN korunmalı (Faz 6.20 kapsam dışı, ayrı DEFERRED maddesi)
        var styleSrc = ExtractDirective(csp!, "style-src");
        styleSrc.Should().Contain("'unsafe-inline'");
    }

    [Fact]
    public async Task Csp_Nonce_IsUniquePerRequest()
    {
        // Faz 6.20: her request taze kriptografik nonce üretmeli.
        var client = _factory.CreateClient();

        var csp1 = (await client.GetAsync("/tr-TR/"))
            .Headers.GetValues("Content-Security-Policy").First();
        var csp2 = (await client.GetAsync("/tr-TR/"))
            .Headers.GetValues("Content-Security-Policy").First();

        var nonce1 = ExtractNonce(csp1);
        var nonce2 = ExtractNonce(csp2);

        nonce1.Should().NotBeNullOrEmpty();
        nonce2.Should().NotBeNullOrEmpty();
        nonce1.Should().NotBe(nonce2);
    }

    private static string ExtractDirective(string csp, string directive)
    {
        var parts = csp.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.FirstOrDefault(p => p.StartsWith(directive + " ", StringComparison.Ordinal)) ?? "";
    }

    private static string ExtractNonce(string csp)
    {
        var match = Regex.Match(csp, @"'nonce-([A-Za-z0-9+/=]+)'");
        return match.Success ? match.Groups[1].Value : "";
    }
}

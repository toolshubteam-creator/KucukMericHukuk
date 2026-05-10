using System.Net;
using System.Text;
using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Tests.Infrastructure.Security;

public class TurnstileVerifierTests
{
    private readonly TurnstileOptions _options = new()
    {
        Enabled = true,
        SiteKey = "1x00000000000000000000AA",
        SecretKey = "1x0000000000000000000000000000000AA",
        VerifyTimeoutMs = 5000
    };

    private TurnstileVerifier CreateSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new TurnstileVerifier(
            httpClient,
            Options.Create(_options),
            NullLogger<TurnstileVerifier>.Instance);
    }

    [Fact]
    public async Task VerifyAsync_returns_true_when_disabled()
    {
        _options.Enabled = false;
        var sut = CreateSut(new StubHandler("{\"success\":false}"));
        var ok = await sut.VerifyAsync("any-token", "1.2.3.4");
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_returns_false_when_token_empty()
    {
        var sut = CreateSut(new StubHandler("{\"success\":true}"));
        var ok = await sut.VerifyAsync("", "1.2.3.4");
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_returns_true_on_cloudflare_success()
    {
        var sut = CreateSut(new StubHandler("{\"success\":true,\"error-codes\":[],\"hostname\":\"localhost\"}"));
        var ok = await sut.VerifyAsync("good-token", "1.2.3.4");
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_returns_false_on_cloudflare_failure()
    {
        var sut = CreateSut(new StubHandler("{\"success\":false,\"error-codes\":[\"invalid-input-response\"]}"));
        var ok = await sut.VerifyAsync("bad-token", "1.2.3.4");
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_returns_false_on_http_exception()
    {
        var sut = CreateSut(new ExceptionHandler());
        var ok = await sut.VerifyAsync("any-token", "1.2.3.4");
        ok.Should().BeFalse();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        public StubHandler(string body) => _body = body;
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        }
    }

    private sealed class ExceptionHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("network down");
    }
}

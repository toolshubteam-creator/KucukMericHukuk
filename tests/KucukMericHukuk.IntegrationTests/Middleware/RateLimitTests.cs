using System.Net;
using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;

namespace KucukMericHukuk.IntegrationTests.Middleware;

public class RateLimitTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public RateLimitTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sitemap_endpoint_is_excluded_from_rate_limiting()
    {
        var client = _factory.CreateClient();

        // 10 ardışık request — DisableRateLimiting attribute sayesinde hepsi 200 dönmeli
        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/sitemap.xml");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Public_homepage_under_global_limit_returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

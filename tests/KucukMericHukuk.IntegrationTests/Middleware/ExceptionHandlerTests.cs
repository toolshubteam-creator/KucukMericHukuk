using System.Net;
using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;

namespace KucukMericHukuk.IntegrationTests.Middleware;

public class ExceptionHandlerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ExceptionHandlerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Normal_request_returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Not_found_route_serves_friendly_page_via_status_code_pages()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/tr-TR/this-route-does-not-exist-12345");

        // UseStatusCodePagesWithReExecute /tr-TR/Error/404'e re-execute eder
        // → ErrorController NotFound view dönerse 404 status korunur
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

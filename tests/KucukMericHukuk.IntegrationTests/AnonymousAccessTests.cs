using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KucukMericHukuk.IntegrationTests;

public class AnonymousAccessTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AnonymousAccessTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/pages")]
    [InlineData("/admin/services")]
    [InlineData("/admin/attorneys")]
    [InlineData("/admin/categories")]
    [InlineData("/admin/tags")]
    public async Task Anonymous_AdminPage_RedirectsToLogin(string url)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/account/login");
    }

    [Fact]
    public async Task LoginPage_Anonymous_Accessible()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/admin/account/login");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}

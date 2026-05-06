using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KucukMericHukuk.IntegrationTests;

public class PageCrudTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public PageCrudTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ValidInput_RedirectsToDetails()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/pages/create");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("PageKey", "integration-test-page"),
            new KeyValuePair<string, string>("DisplayOrder", "1"),
            new KeyValuePair<string, string>("IsActive", "true"),
            new KeyValuePair<string, string>("Translations[0].LanguageCode", "tr-TR"),
            new KeyValuePair<string, string>("Translations[0].Title", "Test Sayfa"),
            new KeyValuePair<string, string>("Translations[0].Content", "<p>Test</p>"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await client.PostAsync("/admin/pages/create", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/pages/details/");
    }

    [Fact]
    public async Task Create_EmptyTitle_ReturnsValidationError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/pages/create");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("PageKey", "test-empty-title"),
            new KeyValuePair<string, string>("DisplayOrder", "1"),
            new KeyValuePair<string, string>("IsActive", "true"),
            new KeyValuePair<string, string>("Translations[0].LanguageCode", "tr-TR"),
            new KeyValuePair<string, string>("Translations[0].Title", ""),
            new KeyValuePair<string, string>("Translations[0].Content", ""),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await client.PostAsync("/admin/pages/create", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("zorunludur");
    }

    [Fact]
    public async Task AntiForgery_MissingToken_Returns400()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("PageKey", "no-token"),
            new KeyValuePair<string, string>("DisplayOrder", "1"),
            new KeyValuePair<string, string>("IsActive", "true"),
            new KeyValuePair<string, string>("Translations[0].LanguageCode", "tr-TR"),
            new KeyValuePair<string, string>("Translations[0].Title", "Test"),
            new KeyValuePair<string, string>("Translations[0].Content", "<p>Test</p>"),
        });

        var response = await client.PostAsync("/admin/pages/create", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}

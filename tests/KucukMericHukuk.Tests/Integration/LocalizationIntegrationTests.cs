using FluentAssertions;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.Tests.Integration;

public class LocalizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LocalizationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Testing env: Program.cs SQL Server registration'ını atlar
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Test-özel SQLite in-memory provider
                var connection = new SqliteConnection("Filename=:memory:");
                connection.Open();

                services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));

                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                ctx.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task RootUrl_ShouldRedirectToDefaultCulture()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Be("/tr-TR");
    }

    [Fact]
    public async Task DefaultCultureUrl_ShouldReturnSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("tr-TR");
    }

    [Fact]
    public async Task InvalidCulture_ShouldReturn404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/xx-XX/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}

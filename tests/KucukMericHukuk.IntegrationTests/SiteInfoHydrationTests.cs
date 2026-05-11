using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.IntegrationTests;

public class SiteInfoHydrationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public SiteInfoHydrationTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SiteInfoOptions_AfterDbSeed_HydratedFromDatabase()
    {
        // Arrange: DB'ye SiteSetting ekle
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<SiteSetting>().Add(new SiteSetting
            {
                Key = SiteSettingKeys.Name,
                Value = "DB Override Name",
                Group = SiteSettingKeys.Groups.SiteInfo,
                DataType = SiteSettingKeys.DataTypes.String,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act: IOptionsSnapshot<SiteInfoOptions> çöz — IConfigureOptions her scope için tetiklenir
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptionsSnapshot<SiteInfoOptions>>().Value;

        // Assert
        options.Name.Should().Be("DB Override Name");
    }
}

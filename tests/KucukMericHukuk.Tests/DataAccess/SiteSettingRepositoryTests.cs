using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Repositories;
using KucukMericHukuk.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.DataAccess;

public class SiteSettingRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public SiteSettingRepositoryTests()
    {
        _factory = new TestDbContextFactory();
    }

    [Fact]
    public async Task UpsertAsync_NewKey_CreatesEntity()
    {
        await using var context = _factory.CreateContext();
        var repo = new SiteSettingRepository(context);

        await repo.UpsertAsync("Name", "Test", "SiteInfo", "string", "desc", 1);
        await context.SaveChangesAsync();

        await using var verify = _factory.CreateContext();
        var saved = await verify.Set<SiteSetting>().FirstAsync(s => s.Key == "Name");
        saved.Value.Should().Be("Test");
        saved.Group.Should().Be("SiteInfo");
        saved.DataType.Should().Be("string");
        saved.Description.Should().Be("desc");
        saved.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_UpdatesValueOnly()
    {
        await using var seed = _factory.CreateContext();
        seed.Set<SiteSetting>().Add(new SiteSetting
        {
            Key = "Name",
            Value = "Eski",
            Group = "SiteInfo",
            DataType = "string",
            Description = "Orijinal açıklama",
            DisplayOrder = 5,
            CreatedAt = DateTime.UtcNow
        });
        await seed.SaveChangesAsync();

        await using var context = _factory.CreateContext();
        var repo = new SiteSettingRepository(context);

        // Yeni grup/dataType/description verilse de mevcut satır korunur, sadece Value değişir
        await repo.UpsertAsync("Name", "Yeni", "DifferentGroup", "url", "Yeni açıklama", 99);
        await context.SaveChangesAsync();

        await using var verify = _factory.CreateContext();
        var saved = await verify.Set<SiteSetting>().FirstAsync(s => s.Key == "Name");
        saved.Value.Should().Be("Yeni");
        saved.Group.Should().Be("SiteInfo");          // korunur
        saved.DataType.Should().Be("string");          // korunur
        saved.Description.Should().Be("Orijinal açıklama"); // korunur
        saved.DisplayOrder.Should().Be(5);             // korunur
    }

    [Fact]
    public async Task GetByKeyAsync_SoftDeleted_ReturnsNull()
    {
        await using var seed = _factory.CreateContext();
        seed.Set<SiteSetting>().Add(new SiteSetting
        {
            Key = "Hidden",
            Value = "Hidden",
            Group = "SiteInfo",
            DataType = "string",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await seed.SaveChangesAsync();

        await using var context = _factory.CreateContext();
        var repo = new SiteSettingRepository(context);

        var result = await repo.GetByKeyAsync("Hidden");
        result.Should().BeNull(); // global query filter
    }

    [Fact]
    public async Task GetByGroupAsync_ReturnsOnlyGroup()
    {
        await using var seed = _factory.CreateContext();
        seed.Set<SiteSetting>().AddRange(
            new SiteSetting { Key = "A", Group = "SiteInfo", DataType = "string", CreatedAt = DateTime.UtcNow },
            new SiteSetting { Key = "B", Group = "SiteInfo", DataType = "string", CreatedAt = DateTime.UtcNow },
            new SiteSetting { Key = "C", Group = "Integration", DataType = "string", CreatedAt = DateTime.UtcNow });
        await seed.SaveChangesAsync();

        await using var context = _factory.CreateContext();
        var repo = new SiteSettingRepository(context);

        var siteInfo = await repo.GetByGroupAsync("SiteInfo");
        siteInfo.Should().HaveCount(2);
        siteInfo.Select(s => s.Key).Should().BeEquivalentTo(new[] { "A", "B" });
    }

    public void Dispose() => _factory.Dispose();
}

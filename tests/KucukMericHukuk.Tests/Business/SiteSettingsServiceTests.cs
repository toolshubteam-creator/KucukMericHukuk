using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.SiteSetting;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace KucukMericHukuk.Tests.Business;

public class SiteSettingsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<SiteSettingUpdateInput> _validator;

    public SiteSettingsServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(SiteSettingMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new SiteSettingUpdateInputValidator();
    }

    private SiteSettingsService CreateSut(AppDbContext context, IMemoryCache? cache = null)
    {
        var uow = new UnitOfWork(context);
        return new SiteSettingsService(
            uow,
            _mapper,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            _validator,
            NullLogger<SiteSettingsService>.Instance);
    }

    private static SiteSetting Build(string key, string? value, string group = "SiteInfo",
        string dataType = "string", int order = 0)
    {
        return new SiteSetting
        {
            Key = key,
            Value = value,
            Group = group,
            DataType = dataType,
            DisplayOrder = order,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task SeedAsync(AppDbContext context, params SiteSetting[] settings)
    {
        context.Set<SiteSetting>().AddRange(settings);
        await context.SaveChangesAsync();
    }

    // ---------------- GetValueAsync ----------------

    [Fact]
    public async Task GetValueAsync_KeyExists_ReturnsValue()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, Build("Name", "Test Hukuk"));

        var sut = CreateSut(context);
        var result = await sut.GetValueAsync("Name");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Test Hukuk");
    }

    [Fact]
    public async Task GetValueAsync_KeyMissing_ReturnsFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetValueAsync("NonExistent");

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.SiteSetting.NotFound);
    }

    [Fact]
    public async Task GetValueAsync_CacheHit_DoesNotHitDbOnSecondCall()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, Build("Name", "Original"));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(context, cache);

        var first = await sut.GetValueAsync("Name");
        first.Value.Should().Be("Original");

        // DB'yi doğrudan değiştir (servisi bypass)
        var entity = context.Set<SiteSetting>().First(s => s.Key == "Name");
        entity.Value = "Changed";
        await context.SaveChangesAsync();

        // Cache hâlâ ilk değeri tutmalı
        var second = await sut.GetValueAsync("Name");
        second.Value.Should().Be("Original");
    }

    // ---------------- UpdateGroupAsync ----------------

    [Fact]
    public async Task UpdateGroupAsync_ValidInput_UpdatesValues()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context,
            Build("Name", "Eski", "SiteInfo"),
            Build("Tagline", "Eski tagline", "SiteInfo"));

        var sut = CreateSut(context);
        var input = new SiteSettingUpdateInput
        {
            Group = "SiteInfo",
            Values = new Dictionary<string, string?>
            {
                ["Name"] = "Yeni",
                ["Tagline"] = "Yeni tagline"
            }
        };

        var result = await sut.UpdateGroupAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        verify.Set<SiteSetting>().First(s => s.Key == "Name").Value.Should().Be("Yeni");
        verify.Set<SiteSetting>().First(s => s.Key == "Tagline").Value.Should().Be("Yeni tagline");
    }

    [Fact]
    public async Task UpdateGroupAsync_InvalidEmail_ReturnsFailure()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, Build("Email", null, "SiteInfo", "email"));

        var sut = CreateSut(context);
        var input = new SiteSettingUpdateInput
        {
            Group = "SiteInfo",
            Values = new Dictionary<string, string?> { ["Email"] = "geçersiz-email" }
        };

        var result = await sut.UpdateGroupAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.SiteSetting.ValidationFailed);
    }

    [Fact]
    public async Task UpdateGroupAsync_InvalidUrl_ReturnsFailure()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, Build("BaseUrl", null, "SiteInfo", "url"));

        var sut = CreateSut(context);
        var input = new SiteSettingUpdateInput
        {
            Group = "SiteInfo",
            Values = new Dictionary<string, string?> { ["BaseUrl"] = "not-a-url" }
        };

        var result = await sut.UpdateGroupAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.SiteSetting.ValidationFailed);
    }

    [Fact]
    public async Task UpdateGroupAsync_GroupNotFound_ReturnsFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = new SiteSettingUpdateInput
        {
            Group = "NonExistentGroup",
            Values = new Dictionary<string, string?> { ["Foo"] = "bar" }
        };

        var result = await sut.UpdateGroupAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.SiteSetting.GroupNotFound);
    }

    [Fact]
    public async Task UpdateGroupAsync_Success_InvalidatesCache()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, Build("Name", "Eski"));

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(context, cache);

        // İlk okuma cache'i doldurur
        await sut.GetValueAsync("Name");

        var input = new SiteSettingUpdateInput
        {
            Group = "SiteInfo",
            Values = new Dictionary<string, string?> { ["Name"] = "Yeni" }
        };
        await sut.UpdateGroupAsync(input);

        // Cache temizlendi → DB'den fresh oku
        var fresh = await sut.GetValueAsync("Name");
        fresh.Value.Should().Be("Yeni");
    }

    [Fact]
    public async Task UpdateGroupAsync_IgnoresKeysOutsideGroup()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context,
            Build("Name", "Eski", "SiteInfo"),
            Build("GoogleAnalyticsId", "Eski-GA", "Integration"));

        var sut = CreateSut(context);
        var input = new SiteSettingUpdateInput
        {
            Group = "SiteInfo",
            Values = new Dictionary<string, string?>
            {
                ["Name"] = "Yeni",
                ["GoogleAnalyticsId"] = "Update edilmemeli"  // farklı grup
            }
        };

        var result = await sut.UpdateGroupAsync(input);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        verify.Set<SiteSetting>().First(s => s.Key == "Name").Value.Should().Be("Yeni");
        verify.Set<SiteSetting>().First(s => s.Key == "GoogleAnalyticsId").Value.Should().Be("Eski-GA");
    }

    // ---------------- GetGroupedAsync ----------------

    [Fact]
    public async Task GetGroupedAsync_ReturnsGroupedSettings()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context,
            Build("Name", "X", "SiteInfo"),
            Build("Tagline", "Y", "SiteInfo"),
            Build("GoogleAnalyticsId", "G-X", "Integration"));

        var sut = CreateSut(context);
        var result = await sut.GetGroupedAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.First(g => g.Group == "SiteInfo").Settings.Should().HaveCount(2);
        result.Value.First(g => g.Group == "Integration").Settings.Should().HaveCount(1);
    }

    public void Dispose() => _factory.Dispose();
}

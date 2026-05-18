using FluentAssertions;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Moq;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// Faz 7.4.3a — RedirectService: insert-time validasyonlar (self/duplicate/cycle)
/// + CheckCycleAsync AJAX endpoint mantığı + cache invalidator çağrımları.
/// </summary>
public class RedirectServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly Mock<IRedirectCacheInvalidator> _cacheMock;

    public RedirectServiceTests()
    {
        _factory = new TestDbContextFactory();
        var config = new TypeAdapterConfig();
        config.Scan(typeof(RedirectMappingConfig).Assembly);
        _mapper = new Mapper(config);
        _cacheMock = new Mock<IRedirectCacheInvalidator>();
    }

    private RedirectService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new RedirectService(uow, _mapper, _cacheMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsIdAndInvalidatesCache()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/tr-TR/eski",
            ToPath = "/tr-TR/yeni",
            StatusCode = 301,
            IsActive = true
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        _cacheMock.Verify(c => c.Invalidate("/tr-TR/eski"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SelfRedirect_ReturnsSelfRedirectError()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/tr-TR/x",
            ToPath = "/tr-TR/x",
            StatusCode = 301,
            IsActive = true
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Redirect.SelfRedirect);
    }

    [Fact]
    public async Task CreateAsync_DuplicateFromPath_ReturnsDuplicateError()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(new Redirect
            {
                FromPath = "/tr-TR/var-olan",
                ToPath = "/tr-TR/hedef-1",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/tr-TR/var-olan",
            ToPath = "/tr-TR/hedef-2",
            StatusCode = 301,
            IsActive = true
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Redirect.DuplicateFromPath);
    }

    /// <summary>
    /// Zincir A→B var, şimdi B→A eklemek isteniyor. Cycle traverse:
    /// from=/B, to=/A → /A başka redirect'in FromPath'i mi? Evet, /A→/B. → /B
    /// → visited'da from olarak /B var → CYCLE.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TwoStepCycle_ReturnsCycleDetected()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.Redirects.Add(new Redirect
            {
                FromPath = "/A",
                ToPath = "/B",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new RedirectFormDto
        {
            FromPath = "/B",
            ToPath = "/A",
            StatusCode = 301,
            IsActive = true
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Redirect.CycleDetected);
    }

    [Fact]
    public async Task CheckCycleAsync_SelfRedirect_ReturnsNotOk()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CheckCycleAsync("/X", "/X");

        result.Ok.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckCycleAsync_CleanChain_ReturnsOk()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CheckCycleAsync("/yeni", "/temiz-hedef");

        result.Ok.Should().BeTrue();
        result.Message.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCacheForFromPath()
    {
        int id;
        await using (var seed = _factory.CreateContext())
        {
            var r = new Redirect
            {
                FromPath = "/silinecek",
                ToPath = "/baska",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            seed.Redirects.Add(r);
            await seed.SaveChangesAsync();
            id = r.Id;
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(c => c.Invalidate("/silinecek"), Times.Once);
    }

    [Fact]
    public async Task ToggleActiveAsync_FlipsAndInvalidatesCache()
    {
        int id;
        await using (var seed = _factory.CreateContext())
        {
            var r = new Redirect
            {
                FromPath = "/toggle",
                ToPath = "/hedef",
                StatusCode = 301,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            seed.Redirects.Add(r);
            await seed.SaveChangesAsync();
            id = r.Id;
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.ToggleActiveAsync(id);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var saved = verify.Redirects.First(r => r.Id == id);
        saved.IsActive.Should().BeFalse();
        _cacheMock.Verify(c => c.Invalidate("/toggle"), Times.Once);
    }

    public void Dispose() => _factory.Dispose();
}

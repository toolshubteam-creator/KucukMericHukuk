using FluentAssertions;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.DTOs.NotFoundLog;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// Faz 7.3.2b — NotFoundService unit testleri. SQLite in-memory + manuel seed.
/// </summary>
public class NotFoundServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly ICurrentUserAccessor _currentUser;

    public NotFoundServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(NotFoundLogMappingConfig).Assembly);
        _mapper = new Mapper(config);

        var mock = new Mock<ICurrentUserAccessor>();
        mock.SetupGet(x => x.UserId).Returns(1);
        mock.SetupGet(x => x.UserName).Returns("admin@test");
        mock.SetupGet(x => x.IpAddress).Returns("127.0.0.1");
        _currentUser = mock.Object;
    }

    private NotFoundService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new NotFoundService(uow, _mapper, _currentUser, NullLogger<NotFoundService>.Instance);
    }

    private static NotFoundLog BuildLog(
        string url,
        int hitCount = 1,
        DateTime? firstSeenAt = null,
        DateTime? lastSeenAt = null)
    {
        var now = DateTime.UtcNow;
        return new NotFoundLog
        {
            Id = Guid.NewGuid(),
            Url = url,
            HitCount = hitCount,
            FirstSeenAt = firstSeenAt ?? now,
            LastSeenAt = lastSeenAt ?? now
        };
    }

    [Fact]
    public async Task GetPagedAsync_DefaultSort_OrdersByHitCountDescending()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.NotFoundLogs.AddRange(
                BuildLog("/url-a", hitCount: 5),
                BuildLog("/url-b", hitCount: 12),
                BuildLog("/url-c", hitCount: 3));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetPagedAsync(new NotFoundLogQueryDto());

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.Items.Select(i => i.Url).Should().ContainInOrder("/url-b", "/url-a", "/url-c");
    }

    [Fact]
    public async Task GetPagedAsync_KeywordFilter_MatchesUrlSubstring()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.NotFoundLogs.AddRange(
                BuildLog("/tr-TR/makaleler/eski"),
                BuildLog("/tr-TR/avukatlar/silinmis"),
                BuildLog("/tr-TR/makaleler/baska"));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetPagedAsync(new NotFoundLogQueryDto { Keyword = "makaleler" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().OnlyContain(n => n.Url.Contains("makaleler"));
    }

    [Fact]
    public async Task PurgeAllAsync_DeletesAllRowsAndReturnsCount()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.NotFoundLogs.AddRange(
                BuildLog("/a"), BuildLog("/b"), BuildLog("/c"));
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.PurgeAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);

        await using var verify = _factory.CreateContext();
        verify.NotFoundLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task PurgeAsync_ExistingId_DeletesSingleRowAndPreservesOthers()
    {
        Guid targetId;
        await using (var seed = _factory.CreateContext())
        {
            var target = BuildLog("/silinecek");
            var keep = BuildLog("/kalan");
            targetId = target.Id;
            seed.NotFoundLogs.AddRange(target, keep);
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.PurgeAsync(targetId);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var remaining = verify.NotFoundLogs.ToList();
        remaining.Should().HaveCount(1);
        remaining[0].Url.Should().Be("/kalan");
    }

    [Fact]
    public async Task PurgeAsync_MissingId_ReturnsFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.PurgeAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("NotFoundLog.NotFound");
    }

    public void Dispose() => _factory.Dispose();
}

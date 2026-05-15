using FluentAssertions;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.AuditLog;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// Faz 7.1 — AuditLogService unit testleri. TestDbContextFactory bare SQLite context kullanır
/// (interceptor wire YOK), kayıtlar manuel seed edilir.
/// </summary>
public class AuditLogServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;

    public AuditLogServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(AuditLogMappingConfig).Assembly);
        _mapper = new Mapper(config);
    }

    private AuditLogService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new AuditLogService(uow, _mapper);
    }

    private static AuditLog BuildAudit(
        string entityName = "Article",
        string entityId = "1",
        AuditActionType action = AuditActionType.Created,
        int? userId = 1,
        string? userName = "admin",
        DateTime? createdAt = null,
        string? changesJson = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            ChangesJson = changesJson,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    private async Task SeedAsync(AppDbContext context, params AuditLog[] items)
    {
        context.AuditLogs.AddRange(items);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPagedAsync_FilterByEntityName_ReturnsMatchingOnly()
    {
        await using var ctx = _factory.CreateContext();
        await SeedAsync(ctx,
            BuildAudit(entityName: "Article", entityId: "1"),
            BuildAudit(entityName: "Article", entityId: "2"),
            BuildAudit(entityName: "Service", entityId: "1"));

        var sut = CreateSut(ctx);
        var result = await sut.GetPagedAsync(new AuditLogQueryDto { EntityName = "Article" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(a => a.EntityName == "Article");
    }

    [Fact]
    public async Task GetPagedAsync_FilterByAction_ReturnsMatchingOnly()
    {
        await using var ctx = _factory.CreateContext();
        await SeedAsync(ctx,
            BuildAudit(action: AuditActionType.Created),
            BuildAudit(action: AuditActionType.Modified),
            BuildAudit(action: AuditActionType.Deleted),
            BuildAudit(action: AuditActionType.Modified));

        var sut = CreateSut(ctx);
        var result = await sut.GetPagedAsync(new AuditLogQueryDto { Action = AuditActionType.Modified });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(a => a.Action == AuditActionType.Modified);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByDateRange_ReturnsInRange()
    {
        await using var ctx = _factory.CreateContext();
        await SeedAsync(ctx,
            BuildAudit(entityId: "before", createdAt: new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc)),
            BuildAudit(entityId: "inRange", createdAt: new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc)),
            BuildAudit(entityId: "after", createdAt: new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc)));

        var sut = CreateSut(ctx);
        var result = await sut.GetPagedAsync(new AuditLogQueryDto
        {
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30)
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].EntityId.Should().Be("inRange");
    }

    [Fact]
    public async Task GetPagedAsync_Keyword_SearchesEntityNameAndUserNameAndEntityId()
    {
        await using var ctx = _factory.CreateContext();
        await SeedAsync(ctx,
            BuildAudit(entityName: "Article", userName: "alice"),
            BuildAudit(entityName: "Service", userName: "bob"),
            BuildAudit(entityName: "Tag", userName: "alice"));

        var sut = CreateSut(ctx);
        var result = await sut.GetPagedAsync(new AuditLogQueryDto { Keyword = "alice" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(a => a.UserName == "alice");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.AuditLog.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsDetail()
    {
        await using var ctx = _factory.CreateContext();
        var audit = BuildAudit(entityName: "Article", changesJson: "{\"Title\":\"Test\"}");
        await SeedAsync(ctx, audit);

        var sut = CreateSut(ctx);
        var result = await sut.GetByIdAsync(audit.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(audit.Id);
        result.Value.EntityName.Should().Be("Article");
        result.Value.ChangesJson.Should().Be("{\"Title\":\"Test\"}");
    }

    public void Dispose() => _factory.Dispose();
}

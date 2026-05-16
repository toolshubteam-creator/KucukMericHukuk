using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Subscriber;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KucukMericHukuk.Tests.Business;

public class SubscriberServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<SubscriberFormDto> _validator;

    public SubscriberServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(SubscriberMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new SubscriberFormValidator();
    }

    private SubscriberService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new SubscriberService(uow, _validator, NullLogger<SubscriberService>.Instance, _mapper);
    }

    private static SubscriberFormDto BuildForm(
        string email = "test@example.com",
        bool kvkk = true,
        string? website = null) => new()
    {
        Email = email,
        KvkkConsent = kvkk,
        Website = website,
        IpAddress = "127.0.0.1",
        UserAgent = "TestAgent"
    };

    // -------------------- SUBSCRIBE --------------------

    [Fact]
    public async Task SubscribeAsync_NewEmail_CreatesActiveSubscriber()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.SubscribeAsync(BuildForm("yeni@example.com"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var sub = verify.Set<Subscriber>().Single(s => s.Email == "yeni@example.com");
        sub.Status.Should().Be(SubscriberStatus.Active);
        sub.UnsubscribeToken.Should().NotBe(Guid.Empty);
        sub.KvkkConsent.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateActiveEmail_ReturnsAlreadySubscribed()
    {
        await using var ctx = _factory.CreateContext();
        ctx.Set<Subscriber>().Add(new Subscriber
        {
            Email = "duplicate@example.com",
            Status = SubscriberStatus.Active,
            UnsubscribeToken = Guid.NewGuid(),
            KvkkConsent = true,
            SubscribedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);

        var result = await sut.SubscribeAsync(BuildForm("duplicate@example.com"));

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Subscriber.AlreadySubscribed);
    }

    [Fact]
    public async Task SubscribeAsync_UnsubscribedEmail_ReactivatesWithNewToken()
    {
        var oldToken = Guid.NewGuid();
        await using var ctx = _factory.CreateContext();
        ctx.Set<Subscriber>().Add(new Subscriber
        {
            Email = "reactivate@example.com",
            Status = SubscriberStatus.Unsubscribed,
            UnsubscribeToken = oldToken,
            UnsubscribedAt = DateTime.UtcNow.AddDays(-7),
            KvkkConsent = true,
            SubscribedAt = DateTime.UtcNow.AddDays(-30)
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);

        var result = await sut.SubscribeAsync(BuildForm("reactivate@example.com"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var sub = verify.Set<Subscriber>().Single(s => s.Email == "reactivate@example.com");
        sub.Status.Should().Be(SubscriberStatus.Active);
        sub.UnsubscribeToken.Should().NotBe(oldToken);
        sub.UnsubscribedAt.Should().BeNull();
    }

    [Fact]
    public async Task SubscribeAsync_SoftDeletedEmail_RestoresAndActivates()
    {
        await using var ctx = _factory.CreateContext();
        ctx.Set<Subscriber>().Add(new Subscriber
        {
            Email = "deleted@example.com",
            Status = SubscriberStatus.Active,
            UnsubscribeToken = Guid.NewGuid(),
            KvkkConsent = true,
            SubscribedAt = DateTime.UtcNow.AddDays(-30),
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);

        var result = await sut.SubscribeAsync(BuildForm("deleted@example.com"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var sub = verify.Set<Subscriber>().IgnoreQueryFilters().Single(s => s.Email == "deleted@example.com");
        sub.IsDeleted.Should().BeFalse();
        sub.Status.Should().Be(SubscriberStatus.Active);
    }

    [Fact]
    public async Task SubscribeAsync_HoneypotFilled_SilentSuccessNoDbInsert()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.SubscribeAsync(BuildForm("bot@example.com", website: "http://spam.com"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        verify.Set<Subscriber>().Should().BeEmpty();
    }

    [Fact]
    public async Task SubscribeAsync_NoKvkkConsent_FailsValidation()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.SubscribeAsync(BuildForm(kvkk: false));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Common.Validation);
    }

    // -------------------- UNSUBSCRIBE BY TOKEN --------------------

    [Fact]
    public async Task UnsubscribeByTokenAsync_ValidToken_MarksUnsubscribed()
    {
        var token = Guid.NewGuid();
        await using var ctx = _factory.CreateContext();
        ctx.Set<Subscriber>().Add(new Subscriber
        {
            Email = "active@example.com",
            Status = SubscriberStatus.Active,
            UnsubscribeToken = token,
            KvkkConsent = true,
            SubscribedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);

        var result = await sut.UnsubscribeByTokenAsync(token);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var sub = verify.Set<Subscriber>().Single(s => s.UnsubscribeToken == token);
        sub.Status.Should().Be(SubscriberStatus.Unsubscribed);
        sub.UnsubscribedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnsubscribeByTokenAsync_InvalidToken_ReturnsInvalidToken()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.UnsubscribeByTokenAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Subscriber.InvalidToken);
    }

    [Fact]
    public async Task UnsubscribeByTokenAsync_AlreadyUnsubscribed_IsIdempotent()
    {
        var token = Guid.NewGuid();
        await using var ctx = _factory.CreateContext();
        ctx.Set<Subscriber>().Add(new Subscriber
        {
            Email = "gone@example.com",
            Status = SubscriberStatus.Unsubscribed,
            UnsubscribeToken = token,
            UnsubscribedAt = DateTime.UtcNow.AddDays(-1),
            KvkkConsent = true,
            SubscribedAt = DateTime.UtcNow.AddDays(-30)
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);

        var result = await sut.UnsubscribeByTokenAsync(token);

        result.IsSuccess.Should().BeTrue();
    }

    public void Dispose() => _factory.Dispose();
}

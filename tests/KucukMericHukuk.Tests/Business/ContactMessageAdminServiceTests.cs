using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Infrastructure.Email;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace KucukMericHukuk.Tests.Business;

public class ContactMessageAdminServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<ContactFormDto> _validator;
    private readonly IValidator<ContactMessageReplyInputDto> _replyValidator;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailSettings> _emailOptions;

    public ContactMessageAdminServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(ContactMessageMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new ContactFormValidator();
        _replyValidator = new ContactMessageReplyInputValidator();
        _emailSender = new Mock<IEmailSender>().Object;
        _emailOptions = Options.Create(new EmailSettings { AdminNotificationEmail = null });
    }

    private ContactMessageService CreateSut(AppDbContext context, IEmailSender? emailSender = null)
    {
        var uow = new UnitOfWork(context);
        return new ContactMessageService(
            uow,
            _validator,
            _replyValidator,
            emailSender ?? _emailSender,
            _emailOptions,
            NullLogger<ContactMessageService>.Instance,
            _mapper);
    }

    private static ContactMessage BuildMessage(
        string name = "Ali Veli",
        string email = "ali@test.com",
        string subject = "Test Konu",
        string message = "Test mesaj içeriği",
        bool isRead = false,
        bool isAnswered = false,
        bool isDeleted = false)
    {
        return new ContactMessage
        {
            Name = name,
            Email = email,
            Subject = subject,
            Message = message,
            KvkkConsent = true,
            IsRead = isRead,
            IsAnswered = isAnswered,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<int> SeedAsync(AppDbContext context, ContactMessage entity)
    {
        context.Set<ContactMessage>().Add(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    // -------------------- GET PAGED --------------------

    [Fact]
    public async Task GetPagedAsync_WithUnreadFilter_FiltersCorrectly()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, BuildMessage(subject: "Yeni 1", isRead: false));
        await SeedAsync(context, BuildMessage(subject: "Yeni 2", isRead: false));
        await SeedAsync(context, BuildMessage(subject: "Okunmuş", isRead: true));

        var sut = CreateSut(context);
        var query = new ContactMessageQueryDto { Status = ContactMessageStatusFilter.Unread };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(m => !m.IsRead);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_FiltersByMultipleFields()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, BuildMessage(name: "Ahmet", email: "ahmet@test.com", subject: "Selam", message: "merhaba"));
        await SeedAsync(context, BuildMessage(name: "Mehmet", email: "mehmet@test.com", subject: "Boşanma davası", message: "Detay yok"));
        await SeedAsync(context, BuildMessage(name: "Veli", email: "veli@test.com", subject: "Test", message: "Boşanma konusu içerikte"));

        var sut = CreateSut(context);
        var query = new ContactMessageQueryDto { Keyword = "Boşanma" };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2); // hem Subject hem Message eşleşmesi
    }

    // -------------------- GET BY ID --------------------

    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.ContactMessage.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsAdminDto()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildMessage(name: "Test Kullanıcı", email: "user@test.com"));

        var sut = CreateSut(context);
        var result = await sut.GetByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Kullanıcı");
        result.Value.Email.Should().Be("user@test.com");
    }

    // -------------------- MARK / TOGGLE --------------------

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_IsIdempotent()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildMessage(isRead: true));

        var sut = CreateSut(context);
        var result = await sut.MarkAsReadAsync(id);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var msg = verify.Set<ContactMessage>().First(m => m.Id == id);
        msg.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleAnsweredAsync_SetsIsReadTrue()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildMessage(isRead: false, isAnswered: false));

        var sut = CreateSut(context);
        var result = await sut.ToggleAnsweredAsync(id);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var msg = verify.Set<ContactMessage>().First(m => m.Id == id);
        msg.IsAnswered.Should().BeTrue();
        msg.IsRead.Should().BeTrue(); // ToggleAnswered IsAnswered=true ise IsRead'i de true yapar
    }

    // -------------------- DELETE / RESTORE --------------------

    [Fact]
    public async Task DeleteAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.DeleteAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.ContactMessage.NotFound);
    }

    [Fact]
    public async Task RestoreAsync_DeletedMessage_UnsetsIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildMessage(isDeleted: true));

        var sut = CreateSut(context);
        var result = await sut.RestoreAsync(id);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var msg = verify.Set<ContactMessage>().IgnoreQueryFilters().First(m => m.Id == id);
        msg.IsDeleted.Should().BeFalse();
        msg.DeletedAt.Should().BeNull();
    }

    // ───────────── Faz 6.7: Reply + Dashboard + Date filter ─────────────

    private async Task<int> SeedMessageAsync(ContactMessage message)
    {
        await using var ctx = _factory.CreateContext();
        ctx.Set<ContactMessage>().Add(message);
        await ctx.SaveChangesAsync();
        return message.Id;
    }

    [Fact]
    public async Task ReplyAsync_ValidInput_CreatesReplyAndMarksAnswered()
    {
        var id = await SeedMessageAsync(BuildMessage(isRead: false, isAnswered: false));

        await using var ctx = _factory.CreateContext();
        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(ctx, emailMock.Object);

        var input = new ContactMessageReplyInputDto
        {
            ContactMessageId = id,
            Body = "Sayin Ali Veli, ilginiz icin tesekkurler."
        };

        var result = await sut.ReplyAsync(input, sentByUserId: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        await using var verify = _factory.CreateContext();
        var msg = verify.Set<ContactMessage>().Include(m => m.Replies).First(m => m.Id == id);
        msg.IsAnswered.Should().BeTrue();
        msg.IsRead.Should().BeTrue();
        msg.Replies.Should().HaveCount(1);
        msg.Replies.First().Body.Should().StartWith("Sayin Ali Veli");
        msg.Replies.First().SentByUserId.Should().BeNull();

        emailMock.Verify(e => e.SendAsync(
            "ali@test.com",
            It.Is<string>(s => s.StartsWith("RE:")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplyAsync_EmptyBody_ReturnsValidationFailure()
    {
        var id = await SeedMessageAsync(BuildMessage());

        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.ReplyAsync(
            new ContactMessageReplyInputDto { ContactMessageId = id, Body = "" },
            sentByUserId: null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.ContactMessage.ReplyBodyRequired);
    }

    [Fact]
    public async Task ReplyAsync_NotFound_ReturnsFailure()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.ReplyAsync(
            new ContactMessageReplyInputDto { ContactMessageId = 9999, Body = "Test yanit." },
            sentByUserId: null);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.ContactMessage.NotFound);
    }

    [Fact]
    public async Task ReplyAsync_EmailSenderThrows_ReplyStillPersists()
    {
        var id = await SeedMessageAsync(BuildMessage());

        await using var ctx = _factory.CreateContext();
        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var sut = CreateSut(ctx, emailMock.Object);

        var result = await sut.ReplyAsync(
            new ContactMessageReplyInputDto { ContactMessageId = id, Body = "Test yanit." },
            sentByUserId: null);

        // Email exception YUTULUR — reply DB'de korunur, success döner
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var msg = verify.Set<ContactMessage>().Include(m => m.Replies).First(m => m.Id == id);
        msg.Replies.Should().HaveCount(1);
        msg.IsAnswered.Should().BeTrue();
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        await SeedMessageAsync(BuildMessage(name: "A", email: "a@x.com", isRead: false));
        await SeedMessageAsync(BuildMessage(name: "B", email: "b@x.com", isRead: false));
        await SeedMessageAsync(BuildMessage(name: "C", email: "c@x.com", isRead: true));

        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var count = await sut.GetUnreadCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsMostRecentN_OrderedDesc()
    {
        // Older
        await SeedMessageAsync(new ContactMessage
        {
            Name = "Older", Email = "older@x.com", Subject = "S1", Message = "M1",
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        // Newer
        await SeedMessageAsync(new ContactMessage
        {
            Name = "Newer", Email = "newer@x.com", Subject = "S2", Message = "M2",
            CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.GetRecentAsync(5);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Newer"); // DESC sıra
        result[1].Name.Should().Be("Older");
    }

    [Fact]
    public async Task GetPagedAsync_DateRangeFilter_ReturnsOnlyInRange()
    {
        await SeedMessageAsync(new ContactMessage
        {
            Name = "Before", Email = "b@x.com", Subject = "S", Message = "M",
            CreatedAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc)
        });
        await SeedMessageAsync(new ContactMessage
        {
            Name = "InRange", Email = "i@x.com", Subject = "S", Message = "M",
            CreatedAt = new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc)
        });
        await SeedMessageAsync(new ContactMessage
        {
            Name = "After", Email = "a@x.com", Subject = "S", Message = "M",
            CreatedAt = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc)
        });

        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var query = new ContactMessageQueryDto
        {
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30)
        };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("InRange");
    }

    [Fact]
    public async Task GetPagedAsync_EndDateInclusive_IncludesLastDay()
    {
        // EndDate 2026-04-30 → repo [start, 2026-05-01) exclusive — yani 2026-04-30 23:59 dahil
        await SeedMessageAsync(new ContactMessage
        {
            Name = "LastSecond", Email = "x@x.com", Subject = "S", Message = "M",
            CreatedAt = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc)
        });

        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var query = new ContactMessageQueryDto
        {
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30)
        };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    public void Dispose() => _factory.Dispose();
}

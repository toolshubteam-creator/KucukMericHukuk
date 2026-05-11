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
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailSettings> _emailOptions;

    public ContactMessageAdminServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(ContactMessageMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new ContactFormValidator();
        _emailSender = new Mock<IEmailSender>().Object;
        _emailOptions = Options.Create(new EmailSettings { AdminNotificationEmail = null });
    }

    private ContactMessageService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new ContactMessageService(
            uow,
            _validator,
            _emailSender,
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

    public void Dispose() => _factory.Dispose();
}

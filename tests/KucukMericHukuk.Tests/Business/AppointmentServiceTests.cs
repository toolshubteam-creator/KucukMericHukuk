using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Appointment;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
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

/// <summary>
/// Faz 6.22 — Randevu modulu servis testleri. ContactMessageAdminServiceTests sablon alindi.
/// </summary>
public class AppointmentServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<AppointmentFormDto> _validator;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailSettings> _emailOptions;

    public AppointmentServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(AppointmentMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new AppointmentFormValidator();
        _emailSender = new Mock<IEmailSender>().Object;
        _emailOptions = Options.Create(new EmailSettings { AdminNotificationEmail = null });
    }

    private AppointmentService CreateSut(
        AppDbContext context,
        IEmailSender? emailSender = null,
        IOptions<EmailSettings>? emailOptions = null)
    {
        var uow = new UnitOfWork(context);
        return new AppointmentService(
            uow,
            _validator,
            emailSender ?? _emailSender,
            emailOptions ?? _emailOptions,
            NullLogger<AppointmentService>.Instance,
            _mapper);
    }

    private static Appointment BuildAppointment(
        string name = "Ali Veli",
        string email = "ali@test.com",
        string phone = "5551112233",
        string subject = "Test Konu",
        AppointmentStatus status = AppointmentStatus.Pending,
        bool isDeleted = false,
        DateTime? createdAt = null)
    {
        return new Appointment
        {
            Name = name,
            Email = email,
            Phone = phone,
            Subject = subject,
            PreferredDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            PreferredTimeNote = "Öğleden sonra",
            Notes = "Test açıklama",
            KvkkConsent = true,
            Status = status,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    private static AppointmentFormDto BuildForm(
        string name = "Ali Veli",
        string email = "ali@test.com",
        string phone = "5551112233",
        string subject = "Test Konu",
        DateOnly? preferredDate = null,
        string? website = null)
    {
        return new AppointmentFormDto
        {
            Name = name,
            Email = email,
            Phone = phone,
            Subject = subject,
            PreferredDate = preferredDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            PreferredTimeNote = "Öğleden sonra",
            Notes = "Test açıklama",
            KvkkConsent = true,
            Website = website
        };
    }

    private async Task<int> SeedAsync(AppDbContext context, Appointment entity)
    {
        context.Set<Appointment>().Add(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    // -------------------- SAVE (PUBLIC) --------------------

    [Fact]
    public async Task SaveAsync_HoneypotFilled_ReturnsSilentSuccessWithoutDbWrite()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.SaveAsync(BuildForm(website: "http://spam.example"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);

        await using var verify = _factory.CreateContext();
        verify.Set<Appointment>().IgnoreQueryFilters().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ValidForm_PersistsWithPendingStatus()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.SaveAsync(BuildForm(name: "Ayşe Yılmaz", email: "Ayse@TEST.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        await using var verify = _factory.CreateContext();
        var appt = verify.Set<Appointment>().First(a => a.Id == result.Value);
        appt.Name.Should().Be("Ayşe Yılmaz");
        appt.Email.Should().Be("ayse@test.com"); // lowercase normalize
        appt.Status.Should().Be(AppointmentStatus.Pending);
        appt.KvkkConsent.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_PastPreferredDate_ReturnsValidationFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var form = BuildForm(preferredDate: DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)));
        var result = await sut.SaveAsync(form);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Common.Validation);

        await using var verify = _factory.CreateContext();
        verify.Set<Appointment>().IgnoreQueryFilters().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_AdminEmailConfigured_SendsNotification()
    {
        await using var context = _factory.CreateContext();
        var emailMock = new Mock<IEmailSender>();
        var emailOptions = Options.Create(new EmailSettings { AdminNotificationEmail = "admin@buro.com" });
        var sut = CreateSut(context, emailMock.Object, emailOptions);

        var result = await sut.SaveAsync(BuildForm());

        result.IsSuccess.Should().BeTrue();
        emailMock.Verify(e => e.SendAsync(
            "admin@buro.com",
            It.Is<string>(s => s.Contains("Yeni Randevu")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------- GET --------------------

    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Appointment.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsAdminDto()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildAppointment(name: "Test Kullanıcı", email: "user@test.com"));

        var sut = CreateSut(context);
        var result = await sut.GetByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Kullanıcı");
        result.Value.Email.Should().Be("user@test.com");
        result.Value.Status.Should().Be(AppointmentStatus.Pending);
    }

    // -------------------- GET PAGED --------------------

    [Fact]
    public async Task GetPagedAsync_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, BuildAppointment(subject: "Bekleyen 1", status: AppointmentStatus.Pending));
        await SeedAsync(context, BuildAppointment(subject: "Bekleyen 2", status: AppointmentStatus.Pending));
        await SeedAsync(context, BuildAppointment(subject: "Onaylı", status: AppointmentStatus.Confirmed));
        await SeedAsync(context, BuildAppointment(subject: "Reddedilmiş", status: AppointmentStatus.Rejected));

        var sut = CreateSut(context);
        var query = new AppointmentQueryDto { Status = AppointmentStatus.Pending };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(a => a.Status == AppointmentStatus.Pending);
    }

    [Fact]
    public async Task GetPagedAsync_NullStatusFilter_ReturnsAllStatuses()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, BuildAppointment(status: AppointmentStatus.Pending));
        await SeedAsync(context, BuildAppointment(status: AppointmentStatus.Confirmed));
        await SeedAsync(context, BuildAppointment(status: AppointmentStatus.Completed));

        var sut = CreateSut(context);
        var result = await sut.GetPagedAsync(new AppointmentQueryDto { Status = null });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPagedAsync_Keyword_FiltersByMultipleFields()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, BuildAppointment(name: "Ahmet", email: "ahmet@test.com", phone: "5550000001", subject: "Selam"));
        await SeedAsync(context, BuildAppointment(name: "Mehmet", email: "mehmet@test.com", phone: "5550000002", subject: "Boşanma davası"));
        await SeedAsync(context, BuildAppointment(name: "Veli Boşanma", email: "veli@test.com", phone: "5550000003", subject: "Test"));

        var sut = CreateSut(context);
        var result = await sut.GetPagedAsync(new AppointmentQueryDto { Keyword = "Boşanma" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2); // hem Subject hem Name eşleşmesi
    }

    [Fact]
    public async Task GetPagedAsync_DateRangeFilter_ReturnsOnlyInRange()
    {
        await using var context = _factory.CreateContext();
        await SeedAsync(context, BuildAppointment(name: "Before", createdAt: new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc)));
        await SeedAsync(context, BuildAppointment(name: "InRange", createdAt: new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc)));
        await SeedAsync(context, BuildAppointment(name: "After", createdAt: new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc)));

        var sut = CreateSut(context);
        var query = new AppointmentQueryDto
        {
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30)
        };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("InRange");
    }

    // -------------------- STATUS TRANSITIONS --------------------

    [Fact]
    public async Task ConfirmAsync_SetsConfirmedWithAdminNote_AndSendsEmail()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildAppointment(email: "muvekkil@test.com"));

        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(context, emailMock.Object);

        var result = await sut.ConfirmAsync(id, "Saat 14:00 uygundur.");

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var appt = verify.Set<Appointment>().First(a => a.Id == id);
        appt.Status.Should().Be(AppointmentStatus.Confirmed);
        appt.AdminNote.Should().Be("Saat 14:00 uygundur.");

        emailMock.Verify(e => e.SendAsync(
            "muvekkil@test.com",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.ConfirmAsync(9999, null);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Appointment.NotFound);
    }

    [Fact]
    public async Task RejectAsync_SetsRejected_AndSendsEmail()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildAppointment(email: "muvekkil@test.com"));

        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(context, emailMock.Object);

        var result = await sut.RejectAsync(id, "Belirtilen tarihte uygun değiliz.");

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var appt = verify.Set<Appointment>().First(a => a.Id == id);
        appt.Status.Should().Be(AppointmentStatus.Rejected);
        appt.AdminNote.Should().Be("Belirtilen tarihte uygun değiliz.");

        emailMock.Verify(e => e.SendAsync(
            "muvekkil@test.com",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_SetsCompleted_AndDoesNotSendEmail()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildAppointment(status: AppointmentStatus.Confirmed));

        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(context, emailMock.Object);

        var result = await sut.CompleteAsync(id);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var appt = verify.Set<Appointment>().First(a => a.Id == id);
        appt.Status.Should().Be(AppointmentStatus.Completed);

        // Completed -> e-posta GONDERILMEZ (karara baglanan nokta)
        emailMock.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_EmailSenderThrows_StatusStillPersists()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildAppointment());

        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var sut = CreateSut(context, emailMock.Object);

        var result = await sut.ConfirmAsync(id, null);

        // Email exception YUTULUR — durum guncellemesi DB'de korunur, success doner
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var appt = verify.Set<Appointment>().First(a => a.Id == id);
        appt.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    // -------------------- DELETE / RESTORE --------------------

    [Fact]
    public async Task DeleteAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.DeleteAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Appointment.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAppointment_SoftDeletes()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildAppointment());

        var sut = CreateSut(context);
        var result = await sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var appt = verify.Set<Appointment>().IgnoreQueryFilters().First(a => a.Id == id);
        appt.IsDeleted.Should().BeTrue();
        appt.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreAsync_DeletedAppointment_UnsetsIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var id = await SeedAsync(context, BuildAppointment(isDeleted: true));

        var sut = CreateSut(context);
        var result = await sut.RestoreAsync(id);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var appt = verify.Set<Appointment>().IgnoreQueryFilters().First(a => a.Id == id);
        appt.IsDeleted.Should().BeFalse();
        appt.DeletedAt.Should().BeNull();
    }

    public void Dispose() => _factory.Dispose();
}

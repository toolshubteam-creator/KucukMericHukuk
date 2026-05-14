using FluentValidation;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Appointment;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.Email;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Business.Services;

/// <summary>
/// Randevu talebi servisi (Faz 6.22). ContactMessageService sablon alindi — reply tablosu YOK,
/// durum yonetimi AppointmentStatus enum uzerinden (Confirm/Reject/Complete).
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _uow;
    private readonly IValidator<AppointmentFormDto> _validator;
    private readonly IEmailSender _emailSender;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IMapper _mapper;

    public AppointmentService(
        IUnitOfWork uow,
        IValidator<AppointmentFormDto> validator,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions,
        ILogger<AppointmentService> logger,
        IMapper mapper)
    {
        _uow = uow;
        _validator = validator;
        _emailSender = emailSender;
        _emailSettings = emailOptions.Value;
        _logger = logger;
        _mapper = mapper;
    }

    // -------------------- PUBLIC --------------------

    public async Task<Result<int>> SaveAsync(AppointmentFormDto form, CancellationToken ct = default)
    {
        // Honeypot: bot doldurmussa silent success (DB'ye yazilmaz, attacker farkina varmasin)
        if (!string.IsNullOrEmpty(form.Website))
        {
            _logger.LogWarning("Appointment honeypot triggered. IP: {IP}, UA: {UA}", form.IpAddress, form.UserAgent);
            return Result.Success(0);
        }

        var validation = await _validator.ValidateAsync(form, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new Error(ErrorCodes.Common.Validation, e.ErrorMessage))
                .ToList();
            return Result.Failure<int>(errors);
        }

        var entity = new Appointment
        {
            Name = form.Name.Trim(),
            Email = form.Email.Trim().ToLowerInvariant(),
            Phone = form.Phone.Trim(),
            Subject = form.Subject.Trim(),
            PreferredDate = form.PreferredDate!.Value,
            PreferredTimeNote = string.IsNullOrWhiteSpace(form.PreferredTimeNote) ? null : form.PreferredTimeNote.Trim(),
            Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim(),
            KvkkConsent = form.KvkkConsent,
            Status = AppointmentStatus.Pending,
            IpAddress = form.IpAddress,
            UserAgent = form.UserAgent
        };

        await _uow.Appointments.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // Admin bildirimi: hatayi SmtpEmailSender icinde yutuyor, form submit etkilenmez.
        if (!string.IsNullOrWhiteSpace(_emailSettings.AdminNotificationEmail))
        {
            var subject = $"[Yeni Randevu Talebi] {entity.Subject}";
            var body = $@"
                <h3>Yeni Randevu Talebi</h3>
                <p><strong>Ad Soyad:</strong> {System.Net.WebUtility.HtmlEncode(entity.Name)}</p>
                <p><strong>E-posta:</strong> {System.Net.WebUtility.HtmlEncode(entity.Email)}</p>
                <p><strong>Telefon:</strong> {System.Net.WebUtility.HtmlEncode(entity.Phone)}</p>
                <p><strong>Konu:</strong> {System.Net.WebUtility.HtmlEncode(entity.Subject)}</p>
                <p><strong>Tercih Edilen Tarih:</strong> {entity.PreferredDate:dd.MM.yyyy}</p>
                <p><strong>Tercih Edilen Saat:</strong> {System.Net.WebUtility.HtmlEncode(entity.PreferredTimeNote ?? "-")}</p>
                <p><strong>Aciklama:</strong></p>
                <p>{System.Net.WebUtility.HtmlEncode(entity.Notes ?? "-").Replace("\n", "<br/>")}</p>
                <hr/>
                <p><small>IP: {entity.IpAddress} | UA: {entity.UserAgent}</small></p>";

            await _emailSender.SendAsync(_emailSettings.AdminNotificationEmail, subject, body, ct);
        }

        return Result.Success(entity.Id);
    }

    // -------------------- ADMIN --------------------

    public async Task<Result<PagedResult<AppointmentListDto>>> GetPagedAsync(
        AppointmentQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Appointments.GetAdminPagedAsync(
            query.Keyword, query.Status, query.IncludeDeleted,
            query.Page, query.PageSize,
            query.StartDate, query.EndDate, ct);

        var dtos = paged.Items.Select(a => _mapper.Map<AppointmentListDto>(a)).ToList();
        var result = new PagedResult<AppointmentListDto>(
            dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<AppointmentAdminDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var appt = await _uow.Appointments.GetByIdIncludingDeletedAsync(id, ct);
        if (appt is null)
        {
            return Result.Failure<AppointmentAdminDto>(
                new Error(ErrorCodes.Appointment.NotFound, "Randevu talebi bulunamadı."));
        }

        return Result.Success(_mapper.Map<AppointmentAdminDto>(appt));
    }

    public async Task<Result> ConfirmAsync(int id, string? adminNote, CancellationToken ct = default)
    {
        var appt = await _uow.Appointments.GetByIdAsync(id, ct);
        if (appt is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Appointment.NotFound, "Randevu talebi bulunamadı."));
        }

        appt.Status = AppointmentStatus.Confirmed;
        appt.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        appt.UpdatedAt = DateTime.UtcNow;
        _uow.Appointments.Update(appt);
        await _uow.SaveChangesAsync(ct);

        await SendStatusEmailAsync(appt, confirmed: true, ct);
        return Result.Success();
    }

    public async Task<Result> RejectAsync(int id, string? adminNote, CancellationToken ct = default)
    {
        var appt = await _uow.Appointments.GetByIdAsync(id, ct);
        if (appt is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Appointment.NotFound, "Randevu talebi bulunamadı."));
        }

        appt.Status = AppointmentStatus.Rejected;
        appt.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        appt.UpdatedAt = DateTime.UtcNow;
        _uow.Appointments.Update(appt);
        await _uow.SaveChangesAsync(ct);

        await SendStatusEmailAsync(appt, confirmed: false, ct);
        return Result.Success();
    }

    public async Task<Result> CompleteAsync(int id, CancellationToken ct = default)
    {
        var appt = await _uow.Appointments.GetByIdAsync(id, ct);
        if (appt is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Appointment.NotFound, "Randevu talebi bulunamadı."));
        }

        // Completed: durum guncellenir, e-posta GONDERILMEZ (karara baglanan nokta).
        appt.Status = AppointmentStatus.Completed;
        appt.UpdatedAt = DateTime.UtcNow;
        _uow.Appointments.Update(appt);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var appt = await _uow.Appointments.GetByIdAsync(id, ct);
        if (appt is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Appointment.NotFound, "Randevu talebi bulunamadı."));
        }

        _uow.Appointments.Delete(appt);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var appt = await _uow.Appointments.GetByIdIncludingDeletedAsync(id, ct);
        if (appt is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Appointment.NotFound, "Randevu talebi bulunamadı."));
        }

        if (!appt.IsDeleted) return Result.Success();

        _uow.Appointments.Restore(appt);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // -------------------- HELPERS --------------------

    /// <summary>
    /// Confirm/Reject sonrasi muvekkile bilgilendirme e-postasi. SmtpEmailSender kendi
    /// exception'larini yutar; yine de defansif try/catch — durum guncellemesi e-postadan etkilenmez.
    /// </summary>
    private async Task SendStatusEmailAsync(Appointment appt, bool confirmed, CancellationToken ct)
    {
        try
        {
            var statusText = confirmed ? "onaylandı" : "reddedildi";
            var subject = $"Randevu talebiniz {statusText}: {appt.Subject}";

            var adminNoteHtml = string.IsNullOrWhiteSpace(appt.AdminNote)
                ? string.Empty
                : $@"<p><strong>Açıklama:</strong></p>
                     <p>{System.Net.WebUtility.HtmlEncode(appt.AdminNote).Replace("\n", "<br/>")}</p>";

            var htmlBody = $@"
                <p>Sayın {System.Net.WebUtility.HtmlEncode(appt.Name)},</p>
                <p>{System.Net.WebUtility.HtmlEncode(appt.PreferredDate.ToString("dd.MM.yyyy"))} tarihli
                   ""{System.Net.WebUtility.HtmlEncode(appt.Subject)}"" konulu randevu talebiniz <strong>{statusText}</strong>.</p>
                {adminNoteHtml}
                <hr/>
                <p><small>Küçükmeriç Hukuk Bürosu</small></p>";

            await _emailSender.SendAsync(appt.Email, subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            // Defansif: SmtpEmailSender icinde zaten try/catch var ama yine de.
            _logger.LogError(ex, "Randevu durum e-postasi beklenmeyen hata: AppointmentId={Id}", appt.Id);
        }
    }
}

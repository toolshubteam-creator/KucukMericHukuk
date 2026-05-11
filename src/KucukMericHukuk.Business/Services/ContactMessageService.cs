using FluentValidation;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.Email;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Business.Services;

public class ContactMessageService : IContactMessageService
{
    private readonly IUnitOfWork _uow;
    private readonly IValidator<ContactFormDto> _validator;
    private readonly IEmailSender _emailSender;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<ContactMessageService> _logger;
    private readonly IMapper _mapper;

    public ContactMessageService(
        IUnitOfWork uow,
        IValidator<ContactFormDto> validator,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions,
        ILogger<ContactMessageService> logger,
        IMapper mapper)
    {
        _uow = uow;
        _validator = validator;
        _emailSender = emailSender;
        _emailSettings = emailOptions.Value;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Result<int>> SaveAsync(ContactFormDto form, CancellationToken ct = default)
    {
        // Honeypot: bot doldurmuşsa silent success (DB'ye yazılmaz, attacker farkına varmasın)
        if (!string.IsNullOrEmpty(form.Website))
        {
            _logger.LogWarning("Honeypot triggered. IP: {IP}, UA: {UA}", form.IpAddress, form.UserAgent);
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

        var entity = new ContactMessage
        {
            Name = form.Name.Trim(),
            Email = form.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim(),
            Subject = form.Subject.Trim(),
            Message = form.Message.Trim(),
            KvkkConsent = form.KvkkConsent,
            IsRead = false,
            IsAnswered = false,
            IpAddress = form.IpAddress,
            UserAgent = form.UserAgent
        };

        await _uow.ContactMessages.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // Admin bildirimi: hatayı SmtpEmailSender içinde yutuyor, form submit etkilenmez.
        if (!string.IsNullOrWhiteSpace(_emailSettings.AdminNotificationEmail))
        {
            var subject = $"[Yeni İletişim] {entity.Subject}";
            var body = $@"
                <h3>Yeni İletişim Mesajı</h3>
                <p><strong>Ad Soyad:</strong> {System.Net.WebUtility.HtmlEncode(entity.Name)}</p>
                <p><strong>E-posta:</strong> {System.Net.WebUtility.HtmlEncode(entity.Email)}</p>
                <p><strong>Telefon:</strong> {System.Net.WebUtility.HtmlEncode(entity.Phone ?? "-")}</p>
                <p><strong>Konu:</strong> {System.Net.WebUtility.HtmlEncode(entity.Subject)}</p>
                <p><strong>Mesaj:</strong></p>
                <p>{System.Net.WebUtility.HtmlEncode(entity.Message).Replace("\n", "<br/>")}</p>
                <hr/>
                <p><small>IP: {entity.IpAddress} | UA: {entity.UserAgent}</small></p>";

            await _emailSender.SendAsync(_emailSettings.AdminNotificationEmail, subject, body, ct);
        }

        return Result.Success(entity.Id);
    }

    // -------------------- ADMIN --------------------

    public async Task<Result<PagedResult<ContactMessageListDto>>> GetPagedAsync(
        ContactMessageQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.ContactMessages.GetAdminPagedAsync(
            query.Keyword, query.Status, query.IncludeDeleted,
            query.Page, query.PageSize, ct);

        var dtos = paged.Items.Select(m => _mapper.Map<ContactMessageListDto>(m)).ToList();
        var result = new PagedResult<ContactMessageListDto>(
            dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<ContactMessageAdminDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var msg = await _uow.ContactMessages.GetByIdIncludingDeletedAsync(id, ct);
        if (msg is null)
        {
            return Result.Failure<ContactMessageAdminDto>(
                new Error(ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        return Result.Success(_mapper.Map<ContactMessageAdminDto>(msg));
    }

    public async Task<Result> MarkAsReadAsync(int id, CancellationToken ct = default)
    {
        var msg = await _uow.ContactMessages.GetByIdIncludingDeletedAsync(id, ct);
        if (msg is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        if (msg.IsRead) return Result.Success();

        msg.IsRead = true;
        _uow.ContactMessages.Update(msg);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ToggleReadAsync(int id, CancellationToken ct = default)
    {
        var msg = await _uow.ContactMessages.GetByIdIncludingDeletedAsync(id, ct);
        if (msg is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        msg.IsRead = !msg.IsRead;
        _uow.ContactMessages.Update(msg);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ToggleAnsweredAsync(int id, CancellationToken ct = default)
    {
        var msg = await _uow.ContactMessages.GetByIdIncludingDeletedAsync(id, ct);
        if (msg is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        msg.IsAnswered = !msg.IsAnswered;
        if (msg.IsAnswered) msg.IsRead = true;
        _uow.ContactMessages.Update(msg);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var msg = await _uow.ContactMessages.GetByIdAsync(id, ct);
        if (msg is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        _uow.ContactMessages.Delete(msg);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var msg = await _uow.ContactMessages.GetByIdIncludingDeletedAsync(id, ct);
        if (msg is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        if (!msg.IsDeleted) return Result.Success();

        _uow.ContactMessages.Restore(msg);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

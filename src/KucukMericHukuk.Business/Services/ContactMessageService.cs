using FluentValidation;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.Email;
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

    public ContactMessageService(
        IUnitOfWork uow,
        IValidator<ContactFormDto> validator,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions,
        ILogger<ContactMessageService> logger)
    {
        _uow = uow;
        _validator = validator;
        _emailSender = emailSender;
        _emailSettings = emailOptions.Value;
        _logger = logger;
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
}

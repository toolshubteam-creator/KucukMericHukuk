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
    private readonly IValidator<ContactMessageReplyInputDto> _replyValidator;
    private readonly IEmailSender _emailSender;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<ContactMessageService> _logger;
    private readonly IMapper _mapper;

    public ContactMessageService(
        IUnitOfWork uow,
        IValidator<ContactFormDto> validator,
        IValidator<ContactMessageReplyInputDto> replyValidator,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions,
        ILogger<ContactMessageService> logger,
        IMapper mapper)
    {
        _uow = uow;
        _validator = validator;
        _replyValidator = replyValidator;
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
            query.Page, query.PageSize,
            query.StartDate, query.EndDate, ct);

        var dtos = paged.Items.Select(m => _mapper.Map<ContactMessageListDto>(m)).ToList();
        var result = new PagedResult<ContactMessageListDto>(
            dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<ContactMessageAdminDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // Replies + SentByUser dahil — Faz 6.7 Details view reply geçmişini gösterir
        var msg = await _uow.ContactMessages.GetByIdWithRepliesAsync(id, ct);
        if (msg is null)
        {
            return Result.Failure<ContactMessageAdminDto>(
                new Error(ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        var dto = _mapper.Map<ContactMessageAdminDto>(msg);
        dto.Replies = msg.Replies
            .OrderByDescending(r => r.SentAt)
            .Select(r => _mapper.Map<ContactMessageReplyDto>(r))
            .ToList();

        return Result.Success(dto);
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

    // ───────────── Faz 6.7 ─────────────

    public async Task<Result<int>> ReplyAsync(
        ContactMessageReplyInputDto input, int? sentByUserId, CancellationToken ct = default)
    {
        var validation = await _replyValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new Error(ErrorCodes.ContactMessage.ReplyBodyRequired, e.ErrorMessage, e.PropertyName))
                .ToList();
            return Result.Failure<int>(errors);
        }

        var msg = await _uow.ContactMessages.GetByIdAsync(input.ContactMessageId, ct);
        if (msg is null)
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.ContactMessage.NotFound, "Mesaj bulunamadı."));
        }

        var reply = new ContactMessageReply
        {
            ContactMessageId = msg.Id,
            Body = input.Body.Trim(),
            SentAt = DateTime.UtcNow,
            SentByUserId = sentByUserId,
        };

        msg.Replies.Add(reply);
        msg.IsRead = true;       // reply yazılınca otomatik okundu
        msg.IsAnswered = true;
        msg.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        // Email gönderim — SmtpEmailSender kendi exception'larını yutar, log'a yazar.
        // NullEmailSender da silent log. Her iki durumda transaction etkilenmez.
        try
        {
            var subject = $"RE: {msg.Subject}";
            var htmlBody = BuildReplyEmailBody(msg, reply);
            await _emailSender.SendAsync(msg.Email, subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            // Defansif: SmtpEmailSender içinde zaten try/catch var ama yine de.
            _logger.LogError(ex, "Reply email beklenmeyen hata: ContactMessageId={Id}", msg.Id);
        }

        return Result.Success(reply.Id);
    }

    public Task<int> GetUnreadCountAsync(CancellationToken ct = default)
        => _uow.ContactMessages.GetUnreadCountAsync(ct);

    public async Task<IReadOnlyList<ContactMessageListDto>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        var messages = await _uow.ContactMessages.GetRecentAsync(count, ct);
        return messages.Select(m => _mapper.Map<ContactMessageListDto>(m)).ToList();
    }

    private static string BuildReplyEmailBody(ContactMessage msg, ContactMessageReply reply)
    {
        // Plain text Body HTML escape edilir, <br/> ile satır sonu
        var bodyHtml = System.Net.WebUtility.HtmlEncode(reply.Body).Replace("\n", "<br/>");
        var originalHtml = System.Net.WebUtility.HtmlEncode(msg.Message).Replace("\n", "<br/>");

        return $@"
            <p>Sayın {System.Net.WebUtility.HtmlEncode(msg.Name)},</p>
            <p>{bodyHtml}</p>
            <hr/>
            <p><small>Aşağıda orijinal mesajınız bulunmaktadır:</small></p>
            <blockquote style=""border-left:3px solid #ccc; padding-left:12px; color:#666;"">
                {originalHtml}
            </blockquote>";
    }
}

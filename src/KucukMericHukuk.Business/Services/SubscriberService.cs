using FluentValidation;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Subscriber;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Business.Services;

public class SubscriberService : ISubscriberService
{
    private readonly IUnitOfWork _uow;
    private readonly IValidator<SubscriberFormDto> _validator;
    private readonly ILogger<SubscriberService> _logger;
    private readonly IMapper _mapper;

    public SubscriberService(
        IUnitOfWork uow,
        IValidator<SubscriberFormDto> validator,
        ILogger<SubscriberService> logger,
        IMapper mapper)
    {
        _uow = uow;
        _validator = validator;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Result> SubscribeAsync(SubscriberFormDto form, CancellationToken ct = default)
    {
        // Honeypot: bot doldurmuşsa silent success (attacker farkına varmasın, DB'ye yazılmaz)
        if (!string.IsNullOrEmpty(form.Website))
        {
            _logger.LogWarning("Subscriber honeypot triggered. IP: {IP}, UA: {UA}",
                form.IpAddress, form.UserAgent);
            return Result.Success();
        }

        var validation = await _validator.ValidateAsync(form, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new Error(ErrorCodes.Common.Validation, e.ErrorMessage))
                .ToList();
            return Result.Failure(errors);
        }

        var normalizedEmail = form.Email.Trim().ToLowerInvariant();
        var existing = await _uow.Subscribers.GetByEmailAsync(normalizedEmail, ct);

        if (existing is not null)
        {
            // Soft-deleted ise — admin silmiş, kullanıcı geri abone olmak istiyor: restore + active
            if (existing.IsDeleted)
            {
                _uow.Subscribers.Restore(existing);
                existing.Status = SubscriberStatus.Active;
                existing.UnsubscribedAt = null;
                existing.UnsubscribeToken = Guid.NewGuid();
                existing.SubscribedAt = DateTime.UtcNow;
                existing.KvkkConsent = form.KvkkConsent;
                existing.IpAddress = form.IpAddress;
                existing.UserAgent = form.UserAgent;
                await _uow.SaveChangesAsync(ct);
                return Result.Success();
            }

            // Unsubscribed → re-activate (yeni token üret, eski token geçersiz)
            if (existing.Status == SubscriberStatus.Unsubscribed)
            {
                existing.Status = SubscriberStatus.Active;
                existing.UnsubscribedAt = null;
                existing.UnsubscribeToken = Guid.NewGuid();
                existing.SubscribedAt = DateTime.UtcNow;
                existing.KvkkConsent = form.KvkkConsent;
                existing.IpAddress = form.IpAddress;
                existing.UserAgent = form.UserAgent;
                _uow.Subscribers.Update(existing);
                await _uow.SaveChangesAsync(ct);
                return Result.Success();
            }

            // Zaten Active — idempotent success (kullanıcıya error göstermeyiz, deneyim için)
            return Result.Failure(new Error(
                ErrorCodes.Subscriber.AlreadySubscribed,
                "Bu e-posta adresi zaten kayıtlı."));
        }

        var entity = new Subscriber
        {
            Email = normalizedEmail,
            Status = SubscriberStatus.Active,
            UnsubscribeToken = Guid.NewGuid(),
            KvkkConsent = form.KvkkConsent,
            SubscribedAt = DateTime.UtcNow,
            IpAddress = form.IpAddress,
            UserAgent = form.UserAgent
        };

        await _uow.Subscribers.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> UnsubscribeByTokenAsync(Guid token, CancellationToken ct = default)
    {
        if (token == Guid.Empty)
        {
            return Result.Failure(new Error(
                ErrorCodes.Subscriber.InvalidToken, "Geçersiz iptal bağlantısı."));
        }

        var sub = await _uow.Subscribers.GetByTokenAsync(token, ct);
        if (sub is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Subscriber.InvalidToken, "Geçersiz iptal bağlantısı."));
        }

        // Idempotent — zaten unsubscribed ise success
        if (sub.Status == SubscriberStatus.Unsubscribed)
        {
            return Result.Success();
        }

        sub.Status = SubscriberStatus.Unsubscribed;
        sub.UnsubscribedAt = DateTime.UtcNow;
        _uow.Subscribers.Update(sub);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    // -------------------- ADMIN --------------------

    public async Task<Result<PagedResult<SubscriberListDto>>> GetPagedAsync(
        SubscriberQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Subscribers.GetAdminPagedAsync(
            query.Keyword, query.Status, query.IncludeDeleted,
            query.Page, query.PageSize,
            query.StartDate, query.EndDate, ct);

        var dtos = paged.Items.Select(s => _mapper.Map<SubscriberListDto>(s)).ToList();
        var result = new PagedResult<SubscriberListDto>(
            dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var sub = await _uow.Subscribers.GetByIdAsync(id, ct);
        if (sub is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Subscriber.NotFound, "Abone bulunamadı."));
        }

        _uow.Subscribers.Delete(sub);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var sub = await _uow.Subscribers.GetByIdIncludingDeletedAsync(id, ct);
        if (sub is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Subscriber.NotFound, "Abone bulunamadı."));
        }

        if (!sub.IsDeleted) return Result.Success();

        _uow.Subscribers.Restore(sub);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

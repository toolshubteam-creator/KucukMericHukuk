using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Subscriber;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface ISubscriberService
{
    // Public
    Task<Result> SubscribeAsync(SubscriberFormDto form, CancellationToken ct = default);

    /// <summary>Token ile abonelikten çıkış. Idempotent — zaten unsubscribed ise success döner.</summary>
    Task<Result> UnsubscribeByTokenAsync(Guid token, CancellationToken ct = default);

    // Admin
    Task<Result<PagedResult<SubscriberListDto>>> GetPagedAsync(
        SubscriberQueryDto query, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(int id, CancellationToken ct = default);
}

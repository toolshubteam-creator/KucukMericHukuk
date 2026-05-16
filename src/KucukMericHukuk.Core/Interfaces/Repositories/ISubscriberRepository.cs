using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Subscriber;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface ISubscriberRepository : IGenericRepository<Subscriber>
{
    Task<PagedResult<Subscriber>> GetAdminPagedAsync(
        string? keyword,
        SubscriberStatus? status,
        bool includeDeleted,
        int page,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);

    /// <summary>Email ile arar — soft-deleted dahil (re-subscribe için).</summary>
    Task<Subscriber?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Token ile arar — public unsubscribe akışı kullanır. Soft-deleted hariç.</summary>
    Task<Subscriber?> GetByTokenAsync(Guid token, CancellationToken ct = default);

    Task<Subscriber?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
}

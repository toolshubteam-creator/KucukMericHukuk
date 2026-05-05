using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IAttorneyRepository : IGenericRepository<Attorney>
{
    Task<IReadOnlyList<Attorney>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);
    Task<Attorney?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<Attorney?> GetByIdWithDetailsAsync(int id, string languageCode, CancellationToken ct = default);
    Task<IReadOnlyList<Attorney>> GetByServiceIdAsync(int serviceId, string languageCode, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);

    Task<bool> AttorneysExistAsync(IEnumerable<int> attorneyIds, CancellationToken ct = default);
    Task<List<Attorney>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    Task<Attorney?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default);
    Task<PagedResult<Attorney>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int? serviceId,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default);
    Task<Attorney?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
    Task<bool> UserIdExistsAsync(int userId, int? excludeAttorneyId, CancellationToken ct = default);
}

using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IFaqRepository : IGenericRepository<Faq>
{
    Task<IReadOnlyList<Faq>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);

    Task<PagedResult<Faq>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default);

    Task<Faq?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default);

    Task<Faq?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
}

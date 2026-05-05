using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IPageRepository : IGenericRepository<Page>
{
    Task<Page?> GetByKeyAsync(string pageKey, CancellationToken ct = default);
    Task<Page?> GetByKeyWithTranslationsAsync(string pageKey, CancellationToken ct = default);
    Task<Page?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);

    Task<Page?> GetByPageKeyAsync(string pageKey, string languageCode, CancellationToken ct = default);
    Task<Page?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default);
    Task<PagedResult<Page>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default);
    Task<Page?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
    Task<bool> PageKeyExistsAsync(string pageKey, int? excludeId = null, CancellationToken ct = default);
}

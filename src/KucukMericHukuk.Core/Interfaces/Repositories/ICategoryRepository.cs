using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IReadOnlyList<Category>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetTopLevelAsync(string languageCode, CancellationToken ct = default);
    Task<Category?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);

    Task<Category?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default);
    Task<PagedResult<Category>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int? parentCategoryId,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default);
    Task<Category?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
    Task<List<int>> GetDescendantIdsAsync(int categoryId, CancellationToken ct = default);
    Task<bool> HasChildrenAsync(int categoryId, CancellationToken ct = default);
    Task<List<Category>> GetAllActiveWithTranslationsAsync(string languageCode, CancellationToken ct = default);
}

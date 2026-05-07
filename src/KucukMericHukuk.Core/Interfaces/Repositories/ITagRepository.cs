using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<IReadOnlyList<Tag>> GetAllOrderedAsync(string languageCode, CancellationToken ct = default);
    Task<Tag?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> GetByArticleIdAsync(int articleId, string languageCode, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);

    Task<Tag?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default);
    Task<PagedResult<Tag>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default);
    Task<Tag?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
    Task<List<Tag>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}

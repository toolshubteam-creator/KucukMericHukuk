using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IArticleRepository : IGenericRepository<Article>
{
    Task<Article?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<Article?> GetByIdWithDetailsAsync(int id, string languageCode, CancellationToken ct = default);
    Task<PagedResult<Article>> GetPublishedPagedAsync(string languageCode, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Article>> GetFeaturedAsync(string languageCode, int count, CancellationToken ct = default);
    Task<PagedResult<Article>> GetByCategoryAsync(int categoryId, string languageCode, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Article>> GetByTagAsync(int tagId, string languageCode, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Article>> GetByAuthorAsync(int authorId, string languageCode, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Article>> GetRecentAsync(string languageCode, int count, CancellationToken ct = default);
    Task IncrementViewCountAsync(int articleId, CancellationToken ct = default);
}

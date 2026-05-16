using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;

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
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);

    Task<PagedResult<Article>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        ArticleStatus? status,
        int? categoryId,
        int page,
        int pageSize,
        bool includeDeleted,
        int? authorIdFilter,
        CancellationToken ct = default);

    Task<Article?> GetByIdForAdminAsync(int id, CancellationToken ct = default);

    Task<Article?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Sitemap için: tüm published article'ları slug + PublishedAt + UpdatedAt ile.
    /// Translation include eder (Slug için), pagination YOK.
    /// </summary>
    Task<IReadOnlyList<Article>> GetAllPublishedForSitemapAsync(string languageCode, CancellationToken ct = default);

    /// <summary>
    /// Faz 7.2b-1: Bülten bekleyen makaleler — Status=Published AND NewsletterSentAt IS NULL.
    /// Translation include eder (Title + Slug için), CreatedAt DESC sıralı.
    /// </summary>
    Task<IReadOnlyList<Article>> GetPendingNewsletterArticlesAsync(string languageCode, CancellationToken ct = default);
}

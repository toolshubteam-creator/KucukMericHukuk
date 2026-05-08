using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IArticleService
{
    Task<IReadOnlyList<ArticleListDto>> GetFeaturedOrRecentAsync(
        string languageCode, int count, CancellationToken ct = default);

    Task<Result<ArticleDetailDto>> GetBySlugAsync(
        string languageCode, string slug, CancellationToken ct = default);

    Task<PagedResult<ArticleListDto>> GetPublishedPagedAsync(
        string languageCode, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<PagedResult<ArticleListDto>> GetByCategoryAsync(
        int categoryId, string languageCode, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<ArticleListDto>> GetRelatedAsync(
        int currentArticleId, int? categoryId, string languageCode, int count, CancellationToken ct = default);

    Task<Result<ArticleAdminDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PagedResult<ArticleAdminDto>>> GetPagedAsync(ArticleQueryDto query, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(ArticleInputDto input, CancellationToken ct = default);
    Task<Result> UpdateAsync(ArticleInputDto input, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> RestoreAsync(int id, CancellationToken ct = default);
    Task<Result> HardDeleteAsync(int id, CancellationToken ct = default);
}

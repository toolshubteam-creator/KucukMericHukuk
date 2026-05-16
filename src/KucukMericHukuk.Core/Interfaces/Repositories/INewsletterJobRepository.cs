using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface INewsletterJobRepository : IGenericRepository<NewsletterJob>
{
    Task<PagedResult<NewsletterJob>> GetHistoryPagedAsync(
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Belirtilen makale için aktif (Pending veya Sending) job var mı — duplicate engelleme.</summary>
    Task<bool> HasActiveJobForArticleAsync(int articleId, CancellationToken ct = default);

    Task<NewsletterJob?> GetByIdWithArticleAsync(int id, CancellationToken ct = default);
}

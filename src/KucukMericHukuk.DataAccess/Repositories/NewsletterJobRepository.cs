using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class NewsletterJobRepository : GenericRepository<NewsletterJob>, INewsletterJobRepository
{
    public NewsletterJobRepository(AppDbContext context) : base(context) { }

    public Task<PagedResult<NewsletterJob>> GetHistoryPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        return _dbSet
            .Include(j => j.Article)
                .ThenInclude(a => a!.Translations)
            .AsNoTracking()
            .OrderByDescending(j => j.CreatedAt)
            .ToPagedListAsync(page, pageSize, ct);
    }

    public Task<bool> HasActiveJobForArticleAsync(int articleId, CancellationToken ct = default)
    {
        return _dbSet.AnyAsync(j =>
            j.ArticleId == articleId &&
            (j.Status == NewsletterJobStatus.Pending || j.Status == NewsletterJobStatus.Sending),
            ct);
    }

    public Task<NewsletterJob?> GetByIdWithArticleAsync(int id, CancellationToken ct = default)
    {
        return _dbSet
            .Include(j => j.Article)
                .ThenInclude(a => a!.Translations)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }
}

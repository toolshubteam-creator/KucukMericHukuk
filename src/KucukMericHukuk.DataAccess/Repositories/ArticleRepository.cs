using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class ArticleRepository : GenericRepository<Article>, IArticleRepository
{
    public ArticleRepository(AppDbContext context) : base(context) { }

    public Task<Article?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .Include(a => a.Category).ThenInclude(c => c!.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Tags).ThenInclude(tag => tag.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(a => a.Status == ArticleStatus.Published
                && a.Translations.Any(t => t.LanguageCode == languageCode && t.Slug == slug), ct);

    public Task<Article?> GetByIdWithDetailsAsync(int id, string languageCode, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .Include(a => a.Category).ThenInclude(c => c!.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Tags).ThenInclude(tag => tag.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<PagedResult<Article>> GetPublishedPagedAsync(string languageCode, int pageNumber, int pageSize, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published && a.PublishedAt != null)
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .Include(a => a.Category).ThenInclude(c => c!.Translations.Where(t => t.LanguageCode == languageCode))
            .OrderByDescending(a => a.PublishedAt)
            .ToPagedListAsync(pageNumber, pageSize, ct);

    public async Task<IReadOnlyList<Article>> GetFeaturedAsync(string languageCode, int count, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published && a.IsFeatured)
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .OrderByDescending(a => a.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public Task<PagedResult<Article>> GetByCategoryAsync(int categoryId, string languageCode, int pageNumber, int pageSize, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published && a.CategoryId == categoryId)
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .OrderByDescending(a => a.PublishedAt)
            .ToPagedListAsync(pageNumber, pageSize, ct);

    public Task<PagedResult<Article>> GetByTagAsync(int tagId, string languageCode, int pageNumber, int pageSize, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published && a.Tags.Any(t => t.Id == tagId))
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .OrderByDescending(a => a.PublishedAt)
            .ToPagedListAsync(pageNumber, pageSize, ct);

    public Task<PagedResult<Article>> GetByAuthorAsync(int authorId, string languageCode, int pageNumber, int pageSize, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published && a.AuthorId == authorId)
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .OrderByDescending(a => a.PublishedAt)
            .ToPagedListAsync(pageNumber, pageSize, ct);

    public async Task<IReadOnlyList<Article>> GetRecentAsync(string languageCode, int count, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published)
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .OrderByDescending(a => a.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public Task IncrementViewCountAsync(int articleId, CancellationToken ct = default)
        => _dbSet
            .Where(a => a.Id == articleId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), ct);

    public Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Set<ArticleTranslation>()
            .Where(t => t.Slug == slug && t.LanguageCode == languageCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.ArticleId != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }

    public async Task<PagedResult<Article>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        ArticleStatus? status,
        int? categoryId,
        int page,
        int pageSize,
        bool includeDeleted,
        int? authorIdFilter,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbSet.IgnoreQueryFilters()
            : _dbSet.AsQueryable();

        query = query
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Author)
            .Include(a => a.Category).ThenInclude(c => c!.Translations.Where(t => t.LanguageCode == languageCode))
            .AsSplitQuery();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        // Faz 6.10: Editor ownership filter (Admin için null → tümü)
        if (authorIdFilter.HasValue)
            query = query.Where(a => a.AuthorId == authorIdFilter.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(a => a.Translations.Any(t =>
                t.LanguageCode == languageCode &&
                (t.Title.Contains(keyword) || (t.Excerpt != null && t.Excerpt.Contains(keyword)))));
        }

        query = query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Article?> GetByIdForAdminAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(a => a.Translations)
            .Include(a => a.Author)
            .Include(a => a.Category).ThenInclude(c => c!.Translations)
            .Include(a => a.Tags).ThenInclude(t => t.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Article?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .Include(a => a.Translations)
            .Include(a => a.Tags)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Article>> GetAllPublishedForSitemapAsync(
        string languageCode, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Where(a => a.Status == ArticleStatus.Published
                     && a.PublishedAt != null
                     && a.PublishedAt <= DateTime.UtcNow
                     && a.Translations.Any(t => t.LanguageCode == languageCode && !string.IsNullOrEmpty(t.Slug)))
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(ct);
    }
}

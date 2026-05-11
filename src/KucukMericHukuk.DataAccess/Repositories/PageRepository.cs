using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class PageRepository : GenericRepository<Page>, IPageRepository
{
    public PageRepository(AppDbContext context) : base(context) { }

    public Task<Page?> GetByKeyAsync(string pageKey, CancellationToken ct = default)
        => Query().AsNoTracking().FirstOrDefaultAsync(p => p.PageKey == pageKey, ct);

    public Task<Page?> GetByKeyWithTranslationsAsync(string pageKey, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.PageKey == pageKey, ct);

    public Task<Page?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(p => p.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(p => p.Translations.Any(t => t.LanguageCode == languageCode && t.Slug == slug), ct);

    public Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Set<PageTranslation>()
            .Where(t => t.Slug == slug && t.LanguageCode == languageCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.PageId != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }

    public Task<Page?> GetByPageKeyAsync(string pageKey, string languageCode, CancellationToken ct = default)
        => _dbSet.AsNoTracking()
            .Include(p => p.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(p => p.PageKey == pageKey, ct);

    public Task<Page?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PagedResult<Page>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbSet.IgnoreQueryFilters()
            : _dbSet.AsQueryable();

        query = query.Include(p =>
            p.Translations.Where(t => t.LanguageCode == languageCode));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                p.Translations.Any(t =>
                    t.LanguageCode == languageCode &&
                    t.Title.Contains(keyword)));
        }

        query = query
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Id);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Page?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> PageKeyExistsAsync(string pageKey, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _dbSet.Where(p => p.PageKey == pageKey);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        return query.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Page>> GetAllActiveForSitemapAsync(
        string languageCode, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Translations.Where(t => t.LanguageCode == languageCode))
            .Where(p => p.IsActive
                     && p.Translations.Any(t => t.LanguageCode == languageCode && !string.IsNullOrEmpty(t.Slug)))
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);
    }
}

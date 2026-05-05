using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
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
}

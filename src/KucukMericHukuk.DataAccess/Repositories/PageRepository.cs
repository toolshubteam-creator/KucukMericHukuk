using KucukMericHukuk.Core.Entities;
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
}

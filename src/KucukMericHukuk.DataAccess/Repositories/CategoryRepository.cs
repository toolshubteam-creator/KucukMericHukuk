using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Category>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(c => c.IsActive)
            .Include(c => c.Translations.Where(t => t.LanguageCode == languageCode))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Category>> GetTopLevelAsync(string languageCode, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(c => c.IsActive && c.ParentCategoryId == null)
            .Include(c => c.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .ThenInclude(sc => sc.Translations.Where(t => t.LanguageCode == languageCode))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);

    public Task<Category?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(c => c.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(c => c.Translations.Any(t => t.LanguageCode == languageCode && t.Slug == slug), ct);

    public Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Set<CategoryTranslation>()
            .Where(t => t.Slug == slug && t.LanguageCode == languageCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.CategoryId != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }
}

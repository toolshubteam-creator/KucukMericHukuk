using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class TagRepository : GenericRepository<Tag>, ITagRepository
{
    public TagRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Tag>> GetAllOrderedAsync(string languageCode, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Include(tag => tag.Translations.Where(t => t.LanguageCode == languageCode))
            .ToListAsync(ct);

    public Task<Tag?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(tag => tag.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(tag => tag.Translations.Any(t => t.LanguageCode == languageCode && t.Slug == slug), ct);

    public async Task<IReadOnlyList<Tag>> GetByArticleIdAsync(int articleId, string languageCode, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(tag => tag.Articles.Any(a => a.Id == articleId))
            .Include(tag => tag.Translations.Where(t => t.LanguageCode == languageCode))
            .ToListAsync(ct);

    public Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Set<TagTranslation>()
            .Where(t => t.Slug == slug && t.LanguageCode == languageCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.TagId != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }
}

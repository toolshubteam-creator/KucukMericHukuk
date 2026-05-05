using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
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

    public Task<Tag?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<Tag>> GetAdminPagedAsync(
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

        query = query.Include(t =>
            t.Translations.Where(tr => tr.LanguageCode == languageCode));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(t =>
                t.Translations.Any(tr =>
                    tr.LanguageCode == languageCode &&
                    tr.Name.Contains(keyword)));
        }

        query = query
            .OrderBy(t => t.Translations
                .Where(tr => tr.LanguageCode == languageCode)
                .Select(tr => tr.Name)
                .FirstOrDefault())
            .ThenByDescending(t => t.Id);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Tag?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
}

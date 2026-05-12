using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class TestimonialRepository : GenericRepository<Testimonial>, ITestimonialRepository
{
    public TestimonialRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Testimonial>> GetActiveOrderedAsync(
        string languageCode, CancellationToken ct = default)
    {
        return await Query().AsNoTracking()
            .Where(t => t.IsActive)
            .Include(t => t.Translations.Where(tr => tr.LanguageCode == languageCode))
            .Where(t => t.Translations.Any(tr => tr.LanguageCode == languageCode))
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Testimonial>> GetFeaturedOrderedAsync(
        string languageCode, int maxCount, CancellationToken ct = default)
    {
        if (maxCount <= 0) maxCount = 3;

        return await Query().AsNoTracking()
            .Where(t => t.IsActive && t.IsFeatured)
            .Include(t => t.Translations.Where(tr => tr.LanguageCode == languageCode))
            .Where(t => t.Translations.Any(tr => tr.LanguageCode == languageCode))
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt)
            .Take(maxCount)
            .ToListAsync(ct);
    }

    public Task<PagedResult<Testimonial>> GetPublicPagedAsync(
        string languageCode, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Query().AsNoTracking()
            .Where(t => t.IsActive)
            .Include(t => t.Translations.Where(tr => tr.LanguageCode == languageCode))
            .Where(t => t.Translations.Any(tr => tr.LanguageCode == languageCode))
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt);

        return query.ToPagedListAsync(page, pageSize, ct);
    }

    public async Task<PagedResult<Testimonial>> GetAdminPagedAsync(
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

        query = query.Include(t => t.Translations.Where(tr => tr.LanguageCode == languageCode));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(t => t.Translations.Any(tr =>
                tr.LanguageCode == languageCode &&
                tr.Content.Contains(k)));
        }

        query = query
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Testimonial?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Testimonial?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
}

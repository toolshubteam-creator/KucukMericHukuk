using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class ServiceRepository : GenericRepository<Service>, IServiceRepository
{
    public ServiceRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Service>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Translations.Where(t => t.LanguageCode == languageCode))
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

    public Task<Service?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(s => s.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(s => s.Translations.Any(t => t.LanguageCode == languageCode && t.Slug == slug), ct);

    public Task<Service?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(s => s.Translations)
            .Include(s => s.Attorneys)
                .ThenInclude(a => a.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Set<ServiceTranslation>()
            .Where(t => t.Slug == slug && t.LanguageCode == languageCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.ServiceId != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }

    public async Task<PagedResult<Service>> GetAdminPagedAsync(
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

        query = query
            .Include(s => s.Translations.Where(tr => tr.LanguageCode == languageCode))
            .Include(s => s.Attorneys)
                .ThenInclude(a => a.Translations.Where(tr => tr.LanguageCode == languageCode))
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(s =>
                s.Translations.Any(tr =>
                    tr.LanguageCode == languageCode &&
                    tr.Name.Contains(keyword)));
        }

        query = query
            .OrderBy(s => s.DisplayOrder)
            .ThenByDescending(s => s.Id);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Service?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
}

using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class AttorneyRepository : GenericRepository<Attorney>, IAttorneyRepository
{
    public AttorneyRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Attorney>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(a => a.IsActive)
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);

    public Task<Attorney?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Services.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
                .ThenInclude(s => s.Translations.Where(t => t.LanguageCode == languageCode))
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Translations.Any(t => t.LanguageCode == languageCode && t.Slug == slug), ct);

    public Task<Attorney?> GetByIdWithDetailsAsync(int id, string languageCode, CancellationToken ct = default)
        => Query().AsNoTracking()
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .Include(a => a.Services).ThenInclude(s => s.Translations.Where(t => t.LanguageCode == languageCode))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Attorney>> GetByServiceIdAsync(int serviceId, string languageCode, CancellationToken ct = default)
        => await Query().AsNoTracking()
            .Where(a => a.IsActive && a.Services.Any(s => s.Id == serviceId))
            .Include(a => a.Translations.Where(t => t.LanguageCode == languageCode))
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);

    public Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Set<AttorneyTranslation>()
            .Where(t => t.Slug == slug && t.LanguageCode == languageCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.AttorneyId != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }

    public async Task<bool> AttorneysExistAsync(IEnumerable<int> attorneyIds, CancellationToken ct = default)
    {
        var ids = attorneyIds.Distinct().ToList();
        if (ids.Count == 0) return true;

        var existingCount = await _dbSet
            .Where(a => ids.Contains(a.Id))
            .CountAsync(ct);

        return existingCount == ids.Count;
    }

    public async Task<List<Attorney>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new List<Attorney>();

        return await _dbSet
            .Where(a => idList.Contains(a.Id))
            .ToListAsync(ct);
    }

    public Task<Attorney?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(a => a.Translations)
            .Include(a => a.Services)
                .ThenInclude(s => s.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<PagedResult<Attorney>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int? serviceId,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbSet.IgnoreQueryFilters()
            : _dbSet.AsQueryable();

        query = query
            .Include(a => a.Translations.Where(tr => tr.LanguageCode == languageCode))
            .Include(a => a.Services)
            .AsSplitQuery();

        if (serviceId.HasValue)
        {
            query = query.Where(a => a.Services.Any(s => s.Id == serviceId.Value));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(a =>
                a.Translations.Any(tr =>
                    tr.LanguageCode == languageCode &&
                    tr.FullName.Contains(keyword)));
        }

        query = query
            .OrderBy(a => a.DisplayOrder)
            .ThenByDescending(a => a.Id);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Attorney?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> UserIdExistsAsync(int userId, int? excludeAttorneyId, CancellationToken ct = default)
    {
        var query = _dbSet.Where(a => a.UserId == userId);
        if (excludeAttorneyId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAttorneyId.Value);
        }
        return await query.AnyAsync(ct);
    }
}

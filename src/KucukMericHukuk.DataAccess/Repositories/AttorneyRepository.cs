using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
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
}

using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
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
        => Query().AsNoTracking()
            .Include(s => s.Translations)
            .Include(s => s.Attorneys)
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
}

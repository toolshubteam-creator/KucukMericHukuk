using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class FaqRepository : GenericRepository<Faq>, IFaqRepository
{
    public FaqRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Faq>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default)
    {
        return await Query().AsNoTracking()
            .Where(f => f.IsActive)
            .Include(f => f.Translations.Where(t => t.LanguageCode == languageCode))
            .Where(f => f.Translations.Any(t => t.LanguageCode == languageCode))
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync(ct);
    }
}

using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
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

    public async Task<PagedResult<Faq>> GetAdminPagedAsync(
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
            .Include(f => f.Translations.Where(t => t.LanguageCode == languageCode));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(f => f.Translations.Any(t =>
                t.LanguageCode == languageCode &&
                (t.Question.Contains(k) || t.Answer.Contains(k))));
        }

        query = query
            .OrderBy(f => f.DisplayOrder)
            .ThenByDescending(f => f.CreatedAt);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Faq?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(f => f.Translations)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<Faq?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .Include(f => f.Translations)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
}

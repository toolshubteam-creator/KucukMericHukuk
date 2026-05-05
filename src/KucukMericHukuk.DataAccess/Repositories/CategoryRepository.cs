using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
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

    public Task<Category?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<Category>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int? parentCategoryId,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbSet.IgnoreQueryFilters()
            : _dbSet.AsQueryable();

        query = query.Include(c =>
            c.Translations.Where(tr => tr.LanguageCode == languageCode));

        if (parentCategoryId.HasValue)
        {
            query = query.Where(c => c.ParentCategoryId == parentCategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c =>
                c.Translations.Any(tr =>
                    tr.LanguageCode == languageCode &&
                    tr.Name.Contains(keyword)));
        }

        query = query
            .OrderBy(c => c.DisplayOrder)
            .ThenByDescending(c => c.Id);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Category?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<int>> GetDescendantIdsAsync(int categoryId, CancellationToken ct = default)
    {
        // In-memory BFS — küçük hiyerarşilerde (≤100 kategori) performant.
        var allCategories = await _dbSet
            .Select(c => new { c.Id, c.ParentCategoryId })
            .ToListAsync(ct);

        var descendants = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(categoryId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = allCategories
                .Where(c => c.ParentCategoryId == currentId)
                .Select(c => c.Id);
            foreach (var childId in children)
            {
                descendants.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return descendants;
    }

    public Task<bool> HasChildrenAsync(int categoryId, CancellationToken ct = default)
        => _dbSet.AnyAsync(c => c.ParentCategoryId == categoryId, ct);
}

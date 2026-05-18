using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class SlugHistoryRepository : ISlugHistoryRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<SlugHistory> _dbSet;

    public SlugHistoryRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.SlugHistories;
    }

    /// <summary>
    /// Slug X → Y → X senaryosu: aynı (EntityType, Lang, OldSlug) için iki satır
    /// oluşur (X'i tekrar bırakırken). `CreatedAt DESC.FirstOrDefault` en yeni
    /// satırı seçer — entity'nin O ANDAKİ güncel slug'ına işaret eder.
    /// </summary>
    public Task<SlugHistory?> FindCurrentAsync(
        SluggedEntityType entityType,
        string languageCode,
        string oldSlug,
        CancellationToken ct = default)
    {
        return _dbSet
            .AsNoTracking()
            .Where(s => s.EntityType == entityType
                     && s.LanguageCode == languageCode
                     && s.OldSlug == oldSlug)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public Task AddAsync(SlugHistory entity, CancellationToken ct = default)
        => _dbSet.AddAsync(entity, ct).AsTask();
}

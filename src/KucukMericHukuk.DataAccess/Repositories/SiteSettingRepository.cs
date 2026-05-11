using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class SiteSettingRepository : GenericRepository<SiteSetting>, ISiteSettingRepository
{
    public SiteSettingRepository(AppDbContext context) : base(context) { }

    public Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(s => s.Key == key, ct);

    public async Task<IReadOnlyList<SiteSetting>> GetByGroupAsync(string group, CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(s => s.Group == group)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Key)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SiteSetting>> GetAllOrderedAsync(CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .OrderBy(s => s.Group)
            .ThenBy(s => s.DisplayOrder)
            .ThenBy(s => s.Key)
            .ToListAsync(ct);

    public async Task UpsertAsync(string key, string? value, string group, string dataType,
        string? description = null, int displayOrder = 0, CancellationToken ct = default)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (existing is null)
        {
            await _dbSet.AddAsync(new SiteSetting
            {
                Key = key,
                Value = value,
                Group = group,
                DataType = dataType,
                Description = description,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow,
            }, ct);
        }
        else
        {
            existing.Value = value;
            _dbSet.Update(existing);
        }
    }
}

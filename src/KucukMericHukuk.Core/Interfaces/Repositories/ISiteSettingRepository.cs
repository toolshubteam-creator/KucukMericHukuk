using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface ISiteSettingRepository : IGenericRepository<SiteSetting>
{
    Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken ct = default);

    Task<IReadOnlyList<SiteSetting>> GetByGroupAsync(string group, CancellationToken ct = default);

    Task<IReadOnlyList<SiteSetting>> GetAllOrderedAsync(CancellationToken ct = default);

    /// <summary>
    /// Key bazlı idempotent upsert. Mevcutsa value günceller (CreatedAt/Group/DataType korur),
    /// yoksa yeni satır oluşturur.
    /// </summary>
    Task UpsertAsync(string key, string? value, string group, string dataType,
        string? description = null, int displayOrder = 0, CancellationToken ct = default);
}

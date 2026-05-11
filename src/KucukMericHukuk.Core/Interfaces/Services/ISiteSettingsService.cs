using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.SiteSetting;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface ISiteSettingsService
{
    Task<Result<string?>> GetValueAsync(string key, CancellationToken ct = default);

    Task<Result<IReadOnlyDictionary<string, string?>>> GetAllAsync(CancellationToken ct = default);

    Task<Result<IReadOnlyList<SiteSettingGroupDto>>> GetGroupedAsync(CancellationToken ct = default);

    Task<Result> UpdateGroupAsync(SiteSettingUpdateInput input, CancellationToken ct = default);

    /// <summary>Service-level memory cache'i temizler. Update sonrası otomatik tetiklenir.</summary>
    void InvalidateCache();
}

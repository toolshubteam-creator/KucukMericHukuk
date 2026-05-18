using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Redirect;

namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// Manuel redirect yönetim servisi (Faz 7.4.3a). Insert/Update sırasında:
///   • SelfRedirect (FromPath == ToPath) reddi
///   • DuplicateFromPath (aynı FromPath ikinci kayıt) reddi
///   • CycleDetected (ToPath zinciri max 10 hop traverse → orijinal FromPath'e dönerse) reddi
///
/// Runtime self-loop koruması middleware'dedir (7.4.2); bu service insert-time
/// "kayıtlanmadan önce" katmanıdır.
/// </summary>
public interface IRedirectService
{
    Task<Result<PagedResult<RedirectListDto>>> GetAdminPagedAsync(
        RedirectQueryDto query, CancellationToken ct = default);

    Task<Result<RedirectListDto>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(RedirectFormDto input, CancellationToken ct = default);

    Task<Result> UpdateAsync(RedirectFormDto input, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result> ToggleActiveAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// AJAX loop-check (admin form): kullanıcı ToPath yazarken canlı uyarı.
    /// `Ok`=true: zincir temiz; false + `Message` validasyon nedeni.
    /// </summary>
    Task<CycleCheckResult> CheckCycleAsync(
        string fromPath, string toPath, int? excludeId = null, CancellationToken ct = default);
}

public record CycleCheckResult(bool Ok, string? Message);

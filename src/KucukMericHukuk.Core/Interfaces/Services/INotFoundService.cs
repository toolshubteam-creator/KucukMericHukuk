using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.NotFoundLog;

namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// 404 takibi servis (Faz 7.3.2b). Yazma yok — NotFoundLoggingMiddleware
/// (Faz 7.3.2a) repo üzerinden upsert ediyor. Bu servis admin liste + manuel
/// temizleme akışlarını sağlar.
/// </summary>
public interface INotFoundService
{
    Task<Result<PagedResult<NotFoundLogListDto>>> GetPagedAsync(
        NotFoundLogQueryDto query, CancellationToken ct = default);

    /// <summary>Tek 404 kaydını sil.</summary>
    Task<Result> PurgeAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tüm 404 kayıtlarını sil — yıkıcı, geri alınamaz. Silinen sayıyı döner.</summary>
    Task<Result<int>> PurgeAllAsync(CancellationToken ct = default);
}

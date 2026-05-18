using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.NotFoundLog;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

/// <summary>
/// 404 hit aggregate repo (Faz 7.3.1). `IGenericRepository` implement etmez
/// (NotFoundLog `BaseEntity` türü değil, soft-delete yok). `RecordHitAsync`
/// race-safe upsert — middleware (Faz 7.3.2) her 404'te çağırır.
/// </summary>
public interface INotFoundLogRepository
{
    /// <summary>
    /// URL aggregate'i upsert eder. Var olan kayıt için `HitCount`++ ve
    /// `LastSeenAt` = now; yoksa yeni satır. Race-safe: paralel iki request
    /// aynı URL için gelirse de tek satır kalır (unique-index + retry).
    /// İçinde `SaveChangesAsync` çağrılır — UoW commit çağrısı GEREKMEZ.
    /// </summary>
    /// <returns>True = yeni satır eklendi; False = mevcut satır artırıldı.</returns>
    Task<bool> RecordHitAsync(
        string url,
        string? referer,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct = default);

    /// <summary>Admin liste — sayfalama + filtreli aggregate satırlar.</summary>
    Task<PagedResult<NotFoundLog>> GetAdminPagedAsync(
        NotFoundLogQueryDto query, CancellationToken ct = default);

    Task<NotFoundLog?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tek kayıt hard-delete. Bulunduysa true, yoksa false.</summary>
    Task<bool> PurgeAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tüm kayıtları hard-delete. Silinen satır sayısını döner.</summary>
    Task<int> PurgeAllAsync(CancellationToken ct = default);
}

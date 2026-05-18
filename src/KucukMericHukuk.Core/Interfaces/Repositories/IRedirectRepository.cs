using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

/// <summary>
/// Manuel redirect repo (Faz 7.4.1). Middleware (7.4.2) `GetByFromPathAsync` ile
/// aktif kayıt arar, hit alırsa `RecordHitAsync` atomik artırır.
/// </summary>
public interface IRedirectRepository
{
    /// <summary>
    /// Aktif redirect kaydı (IsActive=true) FromPath eşleşmesi. Yoksa null.
    /// Middleware lookup için optimize.
    /// </summary>
    Task<Redirect?> GetByFromPathAsync(string fromPath, CancellationToken ct = default);

    /// <summary>
    /// Atomik HitCount++ + LastHitAt=now (ExecuteUpdate). Race-safe, audit'e düşmez
    /// (NotFoundLog.RecordHitAsync pattern). Etkilenen satır varsa true.
    /// </summary>
    Task<bool> RecordHitAsync(int id, CancellationToken ct = default);

    Task<PagedResult<Redirect>> GetAdminPagedAsync(
        RedirectQueryDto query, CancellationToken ct = default);

    Task<Redirect?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>FromPath unique kontrolü — service insert/update öncesi duplicate check.</summary>
    Task<bool> ExistsFromPathAsync(string fromPath, int? excludeId = null, CancellationToken ct = default);

    Task AddAsync(Redirect entity, CancellationToken ct = default);

    void Update(Redirect entity);

    void Delete(Redirect entity);
}

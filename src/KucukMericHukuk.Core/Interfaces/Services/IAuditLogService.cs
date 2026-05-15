using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.AuditLog;
using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// AuditLog servis — yalnızca admin paneli okuma için. Yazma yok
/// (AuditSaveChangesInterceptor otomatik üretiyor).
/// </summary>
public interface IAuditLogService
{
    Task<Result<PagedResult<AuditLogListDto>>> GetPagedAsync(
        AuditLogQueryDto query, CancellationToken ct = default);

    Task<Result<AuditLogDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
}

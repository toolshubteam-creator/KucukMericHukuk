using KucukMericHukuk.Core.DTOs.AuditLog;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

/// <summary>
/// AuditLog repo — readonly semantic. Kayıt ekleme/güncelleme/silme YOK
/// (AuditSaveChangesInterceptor üretiyor, audit kaydı manuel müdahale almaz).
/// </summary>
public interface IAuditLogRepository
{
    Task<PagedResult<AuditLog>> GetAdminPagedAsync(
        AuditLogQueryDto query, CancellationToken ct = default);

    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

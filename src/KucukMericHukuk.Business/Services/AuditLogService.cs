using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.AuditLog;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;

namespace KucukMericHukuk.Business.Services;

/// <summary>
/// Faz 7.1 — AuditLog okuma servisi. Yazma yok (AuditSaveChangesInterceptor üretiyor).
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public AuditLogService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<AuditLogListDto>>> GetPagedAsync(
        AuditLogQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.AuditLogs.GetAdminPagedAsync(query, ct);
        var dtos = paged.Items.Select(a => _mapper.Map<AuditLogListDto>(a)).ToList();
        var result = new PagedResult<AuditLogListDto>(
            dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        return Result.Success(result);
    }

    public async Task<Result<AuditLogDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.AuditLogs.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure<AuditLogDetailDto>(
                new Error(ErrorCodes.AuditLog.NotFound, "Audit kaydı bulunamadı."));
        }

        return Result.Success(_mapper.Map<AuditLogDetailDto>(entity));
    }
}

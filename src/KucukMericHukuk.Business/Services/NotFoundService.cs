using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.NotFoundLog;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Business.Services;

/// <summary>
/// 404 takibi servis (Faz 7.3.2b). NotFoundLog audit ignore listesinde
/// (Faz 7.3.1) — buradaki "temizle" işlemi audit log üretmez; admin eylem
/// Serilog Information satırı olarak iz bırakır.
/// </summary>
public class NotFoundService : INotFoundService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<NotFoundService> _logger;

    public NotFoundService(
        IUnitOfWork uow,
        IMapper mapper,
        ICurrentUserAccessor currentUser,
        ILogger<NotFoundService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<PagedResult<NotFoundLogListDto>>> GetPagedAsync(
        NotFoundLogQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.NotFoundLogs.GetAdminPagedAsync(query, ct);
        var dtos = paged.Items.Select(n => _mapper.Map<NotFoundLogListDto>(n)).ToList();
        var result = new PagedResult<NotFoundLogListDto>(
            dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        return Result.Success(result);
    }

    public async Task<Result<NotFoundLogListDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.NotFoundLogs.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure<NotFoundLogListDto>(
                new Error(ErrorCodes.NotFoundLog.NotFound, "404 kaydı bulunamadı."));
        }
        return Result.Success(_mapper.Map<NotFoundLogListDto>(entity));
    }

    public async Task<Result> PurgeAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await _uow.NotFoundLogs.PurgeAsync(id, ct);
        if (!deleted)
        {
            return Result.Failure(new Error(
                ErrorCodes.NotFoundLog.NotFound, "404 kaydı bulunamadı."));
        }

        _logger.LogInformation(
            "NotFoundLog purged single id={Id} by user={User} (#{UserId})",
            id, _currentUser.UserName ?? "(sistem)", _currentUser.UserId);

        return Result.Success();
    }

    public async Task<Result<int>> PurgeAllAsync(CancellationToken ct = default)
    {
        var count = await _uow.NotFoundLogs.PurgeAllAsync(ct);

        _logger.LogInformation(
            "NotFoundLog purge-all by user={User} (#{UserId}) — {Count} rows deleted",
            _currentUser.UserName ?? "(sistem)", _currentUser.UserId, count);

        return Result.Success(count);
    }
}

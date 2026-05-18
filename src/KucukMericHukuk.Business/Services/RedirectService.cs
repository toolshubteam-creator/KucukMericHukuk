using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;

namespace KucukMericHukuk.Business.Services;

/// <summary>
/// Faz 7.4.3a — Redirect admin CRUD + insert-time validasyon.
/// </summary>
public class RedirectService : IRedirectService
{
    /// <summary>Cycle traverse üst sınırı — admin form pahalı olmasın.</summary>
    private const int MaxCycleHopDepth = 10;

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IRedirectCacheInvalidator _cacheInvalidator;

    public RedirectService(
        IUnitOfWork uow,
        IMapper mapper,
        IRedirectCacheInvalidator cacheInvalidator)
    {
        _uow = uow;
        _mapper = mapper;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<PagedResult<RedirectListDto>>> GetAdminPagedAsync(
        RedirectQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Redirects.GetAdminPagedAsync(query, ct);
        var dtos = paged.Items.Select(r => _mapper.Map<RedirectListDto>(r)).ToList();
        var result = new PagedResult<RedirectListDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        return Result.Success(result);
    }

    public async Task<Result<RedirectListDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Redirects.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure<RedirectListDto>(
                new Error(ErrorCodes.Redirect.NotFound, "Yönlendirme bulunamadı."));
        }
        return Result.Success(_mapper.Map<RedirectListDto>(entity));
    }

    public async Task<Result<int>> CreateAsync(RedirectFormDto input, CancellationToken ct = default)
    {
        var validation = await ValidateAsync(input, excludeId: null, ct);
        if (validation.IsFailure) return Result.Failure<int>(validation.Errors);

        var entity = new Redirect
        {
            FromPath = NormalizePath(input.FromPath),
            ToPath = NormalizePath(input.ToPath),
            StatusCode = input.StatusCode,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Redirects.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        _cacheInvalidator.Invalidate(entity.FromPath);

        return Result.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(RedirectFormDto input, CancellationToken ct = default)
    {
        if (!input.Id.HasValue || input.Id.Value <= 0)
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation, "Güncelleme için ID gerekli.", field: nameof(input.Id)));
        }

        var entity = await _uow.Redirects.GetByIdAsync(input.Id.Value, ct);
        if (entity is null)
        {
            return Result.Failure(new Error(ErrorCodes.Redirect.NotFound, "Yönlendirme bulunamadı."));
        }

        var validation = await ValidateAsync(input, excludeId: input.Id, ct);
        if (validation.IsFailure) return validation;

        var oldFromPath = entity.FromPath;
        entity.FromPath = NormalizePath(input.FromPath);
        entity.ToPath = NormalizePath(input.ToPath);
        entity.StatusCode = input.StatusCode;
        entity.IsActive = input.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _uow.Redirects.Update(entity);
        await _uow.SaveChangesAsync(ct);

        // FromPath değişebileceği için hem eski hem yeni anahtarı invalidate et.
        _cacheInvalidator.Invalidate(oldFromPath);
        if (!string.Equals(oldFromPath, entity.FromPath, StringComparison.Ordinal))
        {
            _cacheInvalidator.Invalidate(entity.FromPath);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Redirects.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure(new Error(ErrorCodes.Redirect.NotFound, "Yönlendirme bulunamadı."));
        }

        _uow.Redirects.Delete(entity);
        await _uow.SaveChangesAsync(ct);

        _cacheInvalidator.Invalidate(entity.FromPath);
        return Result.Success();
    }

    public async Task<Result> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Redirects.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure(new Error(ErrorCodes.Redirect.NotFound, "Yönlendirme bulunamadı."));
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        _uow.Redirects.Update(entity);
        await _uow.SaveChangesAsync(ct);

        _cacheInvalidator.Invalidate(entity.FromPath);
        return Result.Success();
    }

    public async Task<CycleCheckResult> CheckCycleAsync(
        string fromPath, string toPath, int? excludeId = null, CancellationToken ct = default)
    {
        var from = NormalizePath(fromPath);
        var to = NormalizePath(toPath);

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            return new CycleCheckResult(true, null);
        }

        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return new CycleCheckResult(false, "Bir URL kendisine yönlendirilemez.");
        }

        if (await WouldFormCycleAsync(from, to, excludeId, ct))
        {
            return new CycleCheckResult(false, "Bu zincir bir döngü oluşturur.");
        }

        return new CycleCheckResult(true, null);
    }

    // ----------- private -----------

    private async Task<Result> ValidateAsync(RedirectFormDto input, int? excludeId, CancellationToken ct)
    {
        var from = NormalizePath(input.FromPath);
        var to = NormalizePath(input.ToPath);

        if (string.IsNullOrWhiteSpace(from))
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation, "Kaynak URL zorunludur.", field: nameof(input.FromPath)));
        }
        if (string.IsNullOrWhiteSpace(to))
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation, "Hedef URL zorunludur.", field: nameof(input.ToPath)));
        }

        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return Result.Failure(new Error(
                ErrorCodes.Redirect.SelfRedirect,
                "Bir URL kendisine yönlendirilemez.",
                field: nameof(input.ToPath)));
        }

        if (input.StatusCode != 301 && input.StatusCode != 302)
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation,
                "Durum kodu sadece 301 (kalıcı) veya 302 (geçici) olabilir.",
                field: nameof(input.StatusCode)));
        }

        if (await _uow.Redirects.ExistsFromPathAsync(from, excludeId, ct))
        {
            return Result.Failure(new Error(
                ErrorCodes.Redirect.DuplicateFromPath,
                "Bu kaynak URL için zaten bir yönlendirme tanımlı.",
                field: nameof(input.FromPath)));
        }

        if (await WouldFormCycleAsync(from, to, excludeId, ct))
        {
            return Result.Failure(new Error(
                ErrorCodes.Redirect.CycleDetected,
                "Bu zincir bir döngü oluşturur. Hedef URL'yi gözden geçirin.",
                field: nameof(input.ToPath)));
        }

        return Result.Success();
    }

    /// <summary>
    /// ToPath'ten başla → ToPath başka bir redirect'in FromPath'i mi → onun ToPath'i…
    /// max 10 hop. Yeni oluşturulacak (from→to) kuralı zincire eklenmiş varsayılır:
    /// traversal başlangıçta from'a ulaşırsa cycle. excludeId update senaryosunda
    /// kendi kaydını traverse'e dahil etmez. Pasif kayıtlar da zincire dahil
    /// (admin pasifi aktifleştirebilir, hazır cycle olmasın).
    /// </summary>
    private async Task<bool> WouldFormCycleAsync(
        string from, string to, int? excludeId, CancellationToken ct)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { from };
        var current = to;

        for (var hop = 0; hop < MaxCycleHopDepth; hop++)
        {
            if (visited.Contains(current))
            {
                return true;
            }
            visited.Add(current);

            var match = await _uow.Redirects.GetByFromPathAnyAsync(current, ct);
            if (match is null) return false;
            if (excludeId.HasValue && match.Id == excludeId.Value)
            {
                // Update senaryosu: kendi kaydını zincirde geçer (sanki yeniden tanımlanıyor)
                // — devam etmez, çünkü onun ToPath'i zaten yeni input'la değişecek.
                return false;
            }

            current = match.ToPath;
        }
        // 10 hop'tan uzun zincir — güvenli tarafta cycle say.
        return true;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        return path.Trim();
    }
}

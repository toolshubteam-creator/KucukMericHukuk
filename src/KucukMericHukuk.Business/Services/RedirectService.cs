using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.DataAccess.Context;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

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
    private readonly AppDbContext _db;

    public RedirectService(
        IUnitOfWork uow,
        IMapper mapper,
        IRedirectCacheInvalidator cacheInvalidator,
        AppDbContext db)
    {
        _uow = uow;
        _mapper = mapper;
        _cacheInvalidator = cacheInvalidator;
        // Faz 7.4.3a-ek: birleşik liste SlugHistory satırları için 4 translation
        // tablosundan current slug resolve. Business katmanı pattern olarak UoW
        // kullanır; bu istisna admin liste render için tek yerde (RedirectMiddleware
        // de aynı AppDbContext pattern'i kullanıyor — referans).
        _db = db;
    }

    public async Task<Result<PagedResult<RedirectListDto>>> GetAdminPagedAsync(
        RedirectQueryDto query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 30 : query.PageSize;

        var combined = new List<RedirectListDto>();

        // Manuel kaynak.
        if (query.Source != RedirectSource.SlugHistory)
        {
            var manualQuery = new RedirectQueryDto
            {
                Keyword = query.Keyword,
                IsActive = query.IsActive,
                Page = 1,
                PageSize = int.MaxValue
            };
            var manual = await _uow.Redirects.GetAdminPagedAsync(manualQuery, ct);
            combined.AddRange(manual.Items.Select(MapManual));
        }

        // SlugHistory kaynağı — IsActive=false filtre sadece pasif Manual'i ister, SlugHistory atlanır.
        if (query.Source != RedirectSource.Manual && query.IsActive != false)
        {
            var histories = await _uow.SlugHistories.GetFilteredForAdminAsync(query.Keyword, ct);
            var resolutions = await ResolveCurrentSlugBatchAsync(histories, ct);
            foreach (var h in histories)
            {
                var key = (h.EntityType, h.EntityId, h.LanguageCode);
                var currentSlug = resolutions.TryGetValue(key, out var s) ? s : null;
                combined.Add(MapSlugHistory(h, currentSlug));
            }
        }

        // Birleşik sıralama (CreatedAt DESC) + in-memory sayfalama.
        var sorted = combined.OrderByDescending(c => c.CreatedAt).ToList();
        var pageItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Result.Success(new PagedResult<RedirectListDto>(
            pageItems, sorted.Count, page, pageSize));
    }

    public async Task<Result<RedirectListDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Redirects.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure<RedirectListDto>(
                new Error(ErrorCodes.Redirect.NotFound, "Yönlendirme bulunamadı."));
        }
        return Result.Success(MapManual(entity));
    }

    private static RedirectListDto MapManual(Redirect r) => new()
    {
        Id = r.Id,
        Source = RedirectSource.Manual,
        FromPath = r.FromPath,
        ToPath = r.ToPath,
        StatusCode = r.StatusCode,
        IsActive = r.IsActive,
        HitCount = r.HitCount,
        LastHitAt = r.LastHitAt,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static RedirectListDto MapSlugHistory(SlugHistory h, string? currentSlug)
    {
        var segment = UrlSegmentFor(h.EntityType);
        // 4 entity (Article/Service/Attorney/Page) URL'ye dönüşür. Category/Tag
        // route'sız — segment null, FromPath sadece "(Kategori)/eski-slug" gibi
        // metinsel etiketle gösterilir.
        var fromPath = segment is null
            ? $"({h.EntityType})/{h.OldSlug}"
            : $"/{h.LanguageCode}/{segment}/{h.OldSlug}";

        var targetDeleted = currentSlug is null;
        var toPath = segment is not null && currentSlug is not null
            ? $"/{h.LanguageCode}/{segment}/{currentSlug}"
            : null;

        return new RedirectListDto
        {
            Id = h.Id,
            Source = RedirectSource.SlugHistory,
            FromPath = fromPath,
            ToPath = toPath,
            StatusCode = 301,
            IsActive = true,
            HitCount = 0,
            LastHitAt = null,
            CreatedAt = h.CreatedAt,
            UpdatedAt = null,
            EntityType = h.EntityType,
            EntityId = h.EntityId,
            LanguageCode = h.LanguageCode,
            TargetDeleted = targetDeleted
        };
    }

    /// <summary>
    /// Path map'i — RedirectMiddleware'in `TryParseSluggedPath` regex'i ile aynı 4
    /// segment. Category/Tag URL route'a sahip değil (filtre olarak görünür),
    /// SlugHistory satırlarında "—" gösterilir.
    /// </summary>
    private static string? UrlSegmentFor(SluggedEntityType type) => type switch
    {
        SluggedEntityType.Article => "Articles",
        SluggedEntityType.Page => "Pages",
        SluggedEntityType.Service => "Services",
        SluggedEntityType.Attorney => "Attorneys",
        _ => null
    };

    /// <summary>
    /// 4 translation tablosu üzerinden (EntityType, EntityId, Lang) → current slug
    /// batch lookup. AppDbContext global query filter parent IsDeleted=true'leri
    /// gizler → soft-deleted entity dict'e gelmez → caller `TargetDeleted=true`
    /// olarak işaretler. Her entity tipi için ayrı query (max 4 query toplam) —
    /// N+1 yok.
    /// </summary>
    private async Task<Dictionary<(SluggedEntityType, int, string), string?>> ResolveCurrentSlugBatchAsync(
        IReadOnlyList<SlugHistory> histories, CancellationToken ct)
    {
        var result = new Dictionary<(SluggedEntityType, int, string), string?>();
        if (histories.Count == 0) return result;

        foreach (var group in histories.GroupBy(h => h.EntityType))
        {
            var ids = group.Select(h => h.EntityId).ToHashSet();
            var langs = group.Select(h => h.LanguageCode).ToHashSet();

            switch (group.Key)
            {
                case SluggedEntityType.Article:
                    var aRows = await _db.Set<ArticleTranslation>()
                        .AsNoTracking()
                        .Where(t => ids.Contains(t.ArticleId) && langs.Contains(t.LanguageCode))
                        .Select(t => new { EntityId = t.ArticleId, t.LanguageCode, t.Slug })
                        .ToListAsync(ct);
                    foreach (var r in aRows) result[(SluggedEntityType.Article, r.EntityId, r.LanguageCode)] = r.Slug;
                    break;

                case SluggedEntityType.Page:
                    var pRows = await _db.Set<PageTranslation>()
                        .AsNoTracking()
                        .Where(t => ids.Contains(t.PageId) && langs.Contains(t.LanguageCode))
                        .Select(t => new { EntityId = t.PageId, t.LanguageCode, t.Slug })
                        .ToListAsync(ct);
                    foreach (var r in pRows) result[(SluggedEntityType.Page, r.EntityId, r.LanguageCode)] = r.Slug;
                    break;

                case SluggedEntityType.Service:
                    var sRows = await _db.Set<ServiceTranslation>()
                        .AsNoTracking()
                        .Where(t => ids.Contains(t.ServiceId) && langs.Contains(t.LanguageCode))
                        .Select(t => new { EntityId = t.ServiceId, t.LanguageCode, t.Slug })
                        .ToListAsync(ct);
                    foreach (var r in sRows) result[(SluggedEntityType.Service, r.EntityId, r.LanguageCode)] = r.Slug;
                    break;

                case SluggedEntityType.Attorney:
                    var atRows = await _db.Set<AttorneyTranslation>()
                        .AsNoTracking()
                        .Where(t => ids.Contains(t.AttorneyId) && langs.Contains(t.LanguageCode))
                        .Select(t => new { EntityId = t.AttorneyId, t.LanguageCode, t.Slug })
                        .ToListAsync(ct);
                    foreach (var r in atRows) result[(SluggedEntityType.Attorney, r.EntityId, r.LanguageCode)] = r.Slug;
                    break;

                // Category/Tag: URL route'sız — resolve atlanır, FromPath etiket olarak gösterilir.
                default: break;
            }
        }

        return result;
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

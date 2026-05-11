using FluentValidation;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.SiteSetting;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Business.Services;

public class SiteSettingsService : ISiteSettingsService
{
    private const string CacheKeyAll = "site-settings:all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly IValidator<SiteSettingUpdateInput> _validator;
    private readonly ILogger<SiteSettingsService> _logger;

    public SiteSettingsService(
        IUnitOfWork uow,
        IMapper mapper,
        IMemoryCache cache,
        IValidator<SiteSettingUpdateInput> validator,
        ILogger<SiteSettingsService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string?>> GetValueAsync(string key, CancellationToken ct = default)
    {
        var all = await LoadAllCachedAsync(ct);
        return all.TryGetValue(key, out var value)
            ? Result.Success<string?>(value)
            : Result.Failure<string?>(new Error(ErrorCodes.SiteSetting.NotFound, $"Ayar bulunamadı: {key}"));
    }

    public async Task<Result<IReadOnlyDictionary<string, string?>>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await LoadAllCachedAsync(ct);
        return Result.Success<IReadOnlyDictionary<string, string?>>(all);
    }

    public async Task<Result<IReadOnlyList<SiteSettingGroupDto>>> GetGroupedAsync(CancellationToken ct = default)
    {
        var settings = await _uow.SiteSettings.GetAllOrderedAsync(ct);
        var grouped = settings
            .GroupBy(s => s.Group)
            .OrderBy(g => g.Key)
            .Select(g => new SiteSettingGroupDto
            {
                Group = g.Key,
                Settings = g.Select(s => _mapper.Map<SiteSettingDto>(s))
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Key)
                    .ToList()
            })
            .ToList();

        return Result.Success<IReadOnlyList<SiteSettingGroupDto>>(grouped);
    }

    public async Task<Result> UpdateGroupAsync(SiteSettingUpdateInput input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new Error(ErrorCodes.SiteSetting.ValidationFailed, e.ErrorMessage, e.PropertyName))
                .ToList();
            return Result.Failure(errors);
        }

        var groupSettings = await _uow.SiteSettings.GetByGroupAsync(input.Group, ct);
        if (groupSettings.Count == 0)
        {
            return Result.Failure(new Error(
                ErrorCodes.SiteSetting.GroupNotFound,
                $"Grup bulunamadı: {input.Group}"));
        }

        // Sadece grubun kendi key'lerinde update — yabancı key gönderilirse görmezden gel
        var allowedKeys = groupSettings.Select(s => s.Key).ToHashSet();
        foreach (var (key, value) in input.Values)
        {
            if (!allowedKeys.Contains(key)) continue;

            var existing = await _uow.SiteSettings.GetByKeyAsync(key, ct);
            if (existing is null) continue;

            existing.Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            _uow.SiteSettings.Update(existing);
        }

        await _uow.SaveChangesAsync(ct);
        InvalidateCache();
        _logger.LogInformation("SiteSettings grubu güncellendi: {Group}", input.Group);

        return Result.Success();
    }

    public void InvalidateCache() => _cache.Remove(CacheKeyAll);

    private async Task<IReadOnlyDictionary<string, string?>> LoadAllCachedAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKeyAll, out IReadOnlyDictionary<string, string?>? cached) && cached is not null)
        {
            return cached;
        }

        var settings = await _uow.SiteSettings.GetAllOrderedAsync(ct);
        var dict = settings.ToDictionary(s => s.Key, s => s.Value);
        var ro = (IReadOnlyDictionary<string, string?>)dict;

        _cache.Set(CacheKeyAll, ro, CacheTtl);
        return ro;
    }
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;

namespace KucukMericHukuk.Business.Services;

public class SlugService : ISlugService
{
    private const int MaxAttempts = 100;

    private readonly IUnitOfWork _uow;

    public SlugService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<string> GenerateUniqueAsync(
        string title,
        string languageCode,
        SluggedEntityType entityType,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = SlugHelper.Generate(title);
        if (string.IsNullOrEmpty(baseSlug))
        {
            throw new InvalidOperationException(
                "Slug üretilebilecek geçerli karakter bulunamadı. Başlığı kontrol edin.");
        }

        return await ResolveUniqueAsync(baseSlug, languageCode, entityType, excludeId, cancellationToken);
    }

    public async Task<string> EnsureUniqueAsync(
        string desiredSlug,
        string languageCode,
        SluggedEntityType entityType,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = SlugHelper.Generate(desiredSlug);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new InvalidOperationException(
                "Geçerli bir slug girin. (örn. 'icra-hukuku')");
        }

        return await ResolveUniqueAsync(normalized, languageCode, entityType, excludeId, cancellationToken);
    }

    private async Task<string> ResolveUniqueAsync(
        string baseSlug,
        string languageCode,
        SluggedEntityType entityType,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var candidate = baseSlug;
        var suffix = 1;

        while (suffix <= MaxAttempts)
        {
            var exists = await SlugExistsAsync(
                candidate, languageCode, entityType, excludeId, cancellationToken);

            if (!exists)
                return candidate;

            suffix++;
            candidate = $"{baseSlug}-{suffix}";
        }

        throw new InvalidOperationException(
            $"100 denemeden sonra benzersiz slug üretilemedi. Base: '{baseSlug}'");
    }

    private Task<bool> SlugExistsAsync(
        string slug,
        string languageCode,
        SluggedEntityType entityType,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        return entityType switch
        {
            SluggedEntityType.Page =>
                _uow.Pages.SlugExistsAsync(slug, languageCode, excludeId, cancellationToken),
            SluggedEntityType.Service =>
                _uow.Services.SlugExistsAsync(slug, languageCode, excludeId, cancellationToken),
            SluggedEntityType.Attorney =>
                _uow.Attorneys.SlugExistsAsync(slug, languageCode, excludeId, cancellationToken),
            SluggedEntityType.Category =>
                _uow.Categories.SlugExistsAsync(slug, languageCode, excludeId, cancellationToken),
            SluggedEntityType.Tag =>
                _uow.Tags.SlugExistsAsync(slug, languageCode, excludeId, cancellationToken),
            SluggedEntityType.Article =>
                _uow.Articles.SlugExistsAsync(slug, languageCode, excludeId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(entityType))
        };
    }
}

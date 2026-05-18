using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface ISlugService
{
    Task<string> GenerateUniqueAsync(
        string title,
        string languageCode,
        SluggedEntityType entityType,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<string> EnsureUniqueAsync(
        string desiredSlug,
        string languageCode,
        SluggedEntityType entityType,
        int? excludeId = null,
        CancellationToken cancellationToken = default);
}

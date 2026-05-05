using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IPageRepository : IGenericRepository<Page>
{
    Task<Page?> GetByKeyAsync(string pageKey, CancellationToken ct = default);
    Task<Page?> GetByKeyWithTranslationsAsync(string pageKey, CancellationToken ct = default);
    Task<Page?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);
}

using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IReadOnlyList<Category>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetTopLevelAsync(string languageCode, CancellationToken ct = default);
    Task<Category?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);
}

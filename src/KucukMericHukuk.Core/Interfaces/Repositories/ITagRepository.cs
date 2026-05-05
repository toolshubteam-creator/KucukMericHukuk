using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<IReadOnlyList<Tag>> GetAllOrderedAsync(string languageCode, CancellationToken ct = default);
    Task<Tag?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> GetByArticleIdAsync(int articleId, string languageCode, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string languageCode, int? excludeId = null, CancellationToken ct = default);
}

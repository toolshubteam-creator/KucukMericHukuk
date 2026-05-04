using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IServiceRepository : IGenericRepository<Service>
{
    Task<IReadOnlyList<Service>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);
    Task<Service?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<Service?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default);
}

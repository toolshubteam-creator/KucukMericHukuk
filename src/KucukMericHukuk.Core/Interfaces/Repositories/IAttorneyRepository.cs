using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IAttorneyRepository : IGenericRepository<Attorney>
{
    Task<IReadOnlyList<Attorney>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);
    Task<Attorney?> GetBySlugAsync(string languageCode, string slug, CancellationToken ct = default);
    Task<Attorney?> GetByIdWithDetailsAsync(int id, string languageCode, CancellationToken ct = default);
    Task<IReadOnlyList<Attorney>> GetByServiceIdAsync(int serviceId, string languageCode, CancellationToken ct = default);
}

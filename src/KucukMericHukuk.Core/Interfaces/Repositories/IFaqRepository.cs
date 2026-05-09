using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IFaqRepository : IGenericRepository<Faq>
{
    Task<IReadOnlyList<Faq>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);
}

using KucukMericHukuk.Core.DTOs.Faq;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IFaqService
{
    Task<IReadOnlyList<FaqListDto>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);
}

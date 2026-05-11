using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Faq;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IFaqService
{
    // Public
    Task<IReadOnlyList<FaqListDto>> GetActiveOrderedAsync(
        string languageCode, CancellationToken ct = default);

    // Admin
    Task<Result<FaqAdminDto>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Result<PagedResult<FaqAdminDto>>> GetPagedAsync(
        FaqQueryDto query, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(FaqInputDto input, CancellationToken ct = default);

    Task<Result> UpdateAsync(FaqInputDto input, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(int id, CancellationToken ct = default);

    Task<Result> HardDeleteAsync(int id, CancellationToken ct = default);
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Page;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IPageService
{
    Task<Result<PageDetailDto>> GetByPageKeyAsync(
        string pageKey, string languageCode,
        CancellationToken cancellationToken = default);

    Task<Result<PageDetailDto>> GetBySlugAsync(
        string slug, string languageCode,
        CancellationToken cancellationToken = default);

    Task<Result<PageAdminDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<PageAdminDto>>> GetPagedAsync(
        PageQueryDto query, CancellationToken cancellationToken = default);

    Task<Result<int>> CreateAsync(
        PageInputDto input, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        PageInputDto input, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        int id, CancellationToken cancellationToken = default);

    Task<Result> RestoreAsync(
        int id, CancellationToken cancellationToken = default);

    Task<Result> HardDeleteAsync(
        int id, CancellationToken cancellationToken = default);
}

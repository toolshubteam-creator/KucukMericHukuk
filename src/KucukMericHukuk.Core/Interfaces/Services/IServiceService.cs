using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Service;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IServiceService
{
    Task<Result<ServiceDetailDto>> GetBySlugAsync(
        string slug, string languageCode, CancellationToken ct = default);

    Task<Result<ServiceAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<Result<PagedResult<ServiceAdminDto>>> GetPagedAsync(
        ServiceQueryDto query, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(
        ServiceInputDto input, CancellationToken ct = default);

    Task<Result> UpdateAsync(
        ServiceInputDto input, CancellationToken ct = default);

    Task<Result> DeleteAsync(
        int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(
        int id, CancellationToken ct = default);

    Task<Result> HardDeleteAsync(
        int id, CancellationToken ct = default);
}

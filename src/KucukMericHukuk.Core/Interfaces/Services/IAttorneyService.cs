using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IAttorneyService
{
    Task<Result<AttorneyDetailDto>> GetBySlugAsync(
        string slug, string languageCode, CancellationToken ct = default);

    Task<Result<AttorneyAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<Result<PagedResult<AttorneyAdminDto>>> GetPagedAsync(
        AttorneyQueryDto query, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(
        AttorneyInputDto input, CancellationToken ct = default);

    Task<Result> UpdateAsync(
        AttorneyInputDto input, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> RestoreAsync(int id, CancellationToken ct = default);
    Task<Result> HardDeleteAsync(int id, CancellationToken ct = default);
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Tag;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface ITagService
{
    Task<Result<TagListDto>> GetBySlugAsync(
        string slug, string languageCode,
        CancellationToken ct = default);

    Task<Result<TagAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<Result<PagedResult<TagAdminDto>>> GetPagedAsync(
        TagQueryDto query, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(
        TagInputDto input, CancellationToken ct = default);

    Task<Result> UpdateAsync(
        TagInputDto input, CancellationToken ct = default);

    Task<Result> DeleteAsync(
        int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(
        int id, CancellationToken ct = default);

    Task<Result> HardDeleteAsync(
        int id, CancellationToken ct = default);
}

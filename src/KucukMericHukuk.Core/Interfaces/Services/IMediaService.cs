using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Media;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IMediaService
{
    Task<Result<MediaFileDto>> UploadAsync(MediaUploadInputDto input, CancellationToken ct = default);
    Task<Result<MediaFileDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PagedResult<MediaFileListDto>>> GetPagedAsync(string? keyword, int page, int pageSize, bool includeDeleted, CancellationToken ct = default);
    Task<Result> UpdateAsync(MediaUpdateInputDto input, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> RestoreAsync(int id, CancellationToken ct = default);
    Task<Result> HardDeleteAsync(int id, CancellationToken ct = default);
}

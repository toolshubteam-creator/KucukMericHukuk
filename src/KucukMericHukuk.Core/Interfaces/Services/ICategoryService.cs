using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Category;
using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<CategoryListDto>> GetBySlugAsync(
        string slug, string languageCode, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryListDto>> GetActiveOrderedAsync(
        string languageCode, CancellationToken ct = default);

    Task<Result<CategoryAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<Result<PagedResult<CategoryAdminDto>>> GetPagedAsync(
        CategoryQueryDto query, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(
        CategoryInputDto input, CancellationToken ct = default);

    Task<Result> UpdateAsync(
        CategoryInputDto input, CancellationToken ct = default);

    Task<Result> DeleteAsync(
        int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(
        int id, CancellationToken ct = default);

    Task<Result> HardDeleteAsync(
        int id, CancellationToken ct = default);
}

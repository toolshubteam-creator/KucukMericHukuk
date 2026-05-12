using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Testimonial;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface ITestimonialService
{
    // Public
    Task<IReadOnlyList<TestimonialListDto>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);

    Task<IReadOnlyList<TestimonialListDto>> GetFeaturedOrderedAsync(string languageCode, int maxCount, CancellationToken ct = default);

    Task<PagedResult<TestimonialListDto>> GetPublicPagedAsync(string languageCode, int page, int pageSize, CancellationToken ct = default);

    // Admin
    Task<Result<TestimonialAdminDto>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Result<PagedResult<TestimonialAdminDto>>> GetPagedAsync(TestimonialQueryDto query, CancellationToken ct = default);

    Task<Result<int>> CreateAsync(TestimonialInputDto input, CancellationToken ct = default);

    Task<Result> UpdateAsync(TestimonialInputDto input, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(int id, CancellationToken ct = default);

    Task<Result> HardDeleteAsync(int id, CancellationToken ct = default);
}

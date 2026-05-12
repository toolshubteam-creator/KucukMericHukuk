using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface ITestimonialRepository : IGenericRepository<Testimonial>
{
    /// <summary>Public liste: aktif + verilen dilde translation olan, DisplayOrder ASC.</summary>
    Task<IReadOnlyList<Testimonial>> GetActiveOrderedAsync(string languageCode, CancellationToken ct = default);

    /// <summary>Featured (öne çıkarılmış) testimonial'lar, Ana Sayfa carousel için.</summary>
    Task<IReadOnlyList<Testimonial>> GetFeaturedOrderedAsync(string languageCode, int maxCount, CancellationToken ct = default);

    /// <summary>Public sayfalı liste — /tr-TR/Referanslar için.</summary>
    Task<PagedResult<Testimonial>> GetPublicPagedAsync(string languageCode, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Admin paneli sayfalı liste — keyword filter (Translation.Content), silinmiş dahil opsiyonu.</summary>
    Task<PagedResult<Testimonial>> GetAdminPagedAsync(
        string? keyword,
        string languageCode,
        int page,
        int pageSize,
        bool includeDeleted,
        CancellationToken ct = default);

    Task<Testimonial?> GetByIdWithTranslationsAsync(int id, CancellationToken ct = default);

    Task<Testimonial?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
}

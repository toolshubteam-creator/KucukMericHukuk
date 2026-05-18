using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// Slug değişim izi servis (Faz 7.4.3a). 6 servis (Article/Page/Service/Attorney/
/// Category/Tag) update akışında çağırır. RedirectMiddleware (Faz 7.4.2) bu iz
/// üzerinden eski URL'leri entity'nin güncel slug'ına 301 yönlendirir.
/// </summary>
public interface ISlugHistoryService
{
    /// <summary>
    /// Slug değişimini SlugHistory'ye ekler (caller UoW SaveChanges'inde commit edilir,
    /// servis kendi SaveChanges çağırmaz — atomic translation + slug history insert).
    ///
    /// No-op koşulları:
    /// - <paramref name="oldSlug"/> null/boş (yeni entity create — eski yok)
    /// - <paramref name="oldSlug"/> == <paramref name="newSlug"/> (gerçek değişim yok)
    /// </summary>
    Task RecordIfChangedAsync(
        SluggedEntityType entityType,
        int entityId,
        string languageCode,
        string? oldSlug,
        string newSlug,
        CancellationToken ct = default);
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

/// <summary>
/// SlugHistory repo (Faz 7.4.1). Yazma: 7.4.3 slug-change service çağırır.
/// Okuma: 7.4.2 RedirectMiddleware "eski slug → yeni slug" lookup'u.
/// </summary>
public interface ISlugHistoryRepository
{
    /// <summary>
    /// Verilen (entityType, languageCode, oldSlug) için EN YENİ kaydı döner
    /// (`CreatedAt DESC.FirstOrDefault`). Slug X→Y→X senaryosunda en güncel
    /// satır seçilir. Yoksa null.
    /// </summary>
    Task<SlugHistory?> FindCurrentAsync(
        SluggedEntityType entityType,
        string languageCode,
        string oldSlug,
        CancellationToken ct = default);

    Task AddAsync(SlugHistory entity, CancellationToken ct = default);
}

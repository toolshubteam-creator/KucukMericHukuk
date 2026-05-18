using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Core.Entities;

/// <summary>
/// Slug değişim izi (Faz 7.4.1). Bir entity'nin slug'ı değiştiğinde sistem
/// otomatik kayıt eder (Faz 7.4.3 — 6 servis update); `RedirectMiddleware`
/// (Faz 7.4.2) bu izden eski slug'ı yakalayıp entity'nin GÜNCEL slug'ına 301
/// üretir — manuel `Redirect` kuralı gerekmez.
///
/// BaseEntity DEĞİL, audit ignore listesinde (sistem üretir).
///
/// Composite index (EntityType, LanguageCode, OldSlug) NON-UNIQUE: slug X→Y→X
/// senaryosunda iki satır oluşabilir. Repo `FindCurrentAsync` `CreatedAt DESC`
/// ile en yeni eşleşmeyi döner.
/// </summary>
public class SlugHistory
{
    public int Id { get; set; }

    public SluggedEntityType EntityType { get; set; }

    public int EntityId { get; set; }

    /// <summary>Translation tablosu LanguageCode (max 10, örn. "tr-TR").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Değiştirilmeden önceki slug değeri (max 200).</summary>
    public string OldSlug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

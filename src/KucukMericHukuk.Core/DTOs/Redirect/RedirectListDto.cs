using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Core.DTOs.Redirect;

/// <summary>
/// Admin "Yönlendirmeler" listesi satırı (Faz 7.4.3a-ek).
/// İki kaynaktan beslenir: <see cref="RedirectSource.Manual"/> (admin CRUD) ve
/// <see cref="RedirectSource.SlugHistory"/> (servis otomatik üretti, salt-okunur).
///
/// Manual satır: `Id` = Redirect tablosu primary key, tüm CRUD erişimi açık.
/// SlugHistory satır: `Id` = SlugHistory tablosu primary key, CRUD KAPALI
/// (UI <see cref="IsReadOnly"/> bayrağı ile butonları gizler).
/// </summary>
public class RedirectListDto
{
    public int Id { get; set; }
    public RedirectSource Source { get; set; }

    public string FromPath { get; set; } = string.Empty;
    public string? ToPath { get; set; }
    public int StatusCode { get; set; }
    public bool IsActive { get; set; }
    public int HitCount { get; set; }
    public DateTime? LastHitAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // SlugHistory-spesifik alanlar (Manual satırlarında null).
    public SluggedEntityType? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? LanguageCode { get; set; }

    /// <summary>
    /// SlugHistory satırında parent entity soft-deleted veya translation
    /// bulunamadı → UI "hedef silinmiş" rozeti göster, `ToPath` null.
    /// </summary>
    public bool TargetDeleted { get; set; }

    /// <summary>UI butonları (düzenle/sil/toggle) gizlenmeli mi.</summary>
    public bool IsReadOnly => Source == RedirectSource.SlugHistory;
}

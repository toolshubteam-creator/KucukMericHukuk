namespace KucukMericHukuk.Core.Entities;

/// <summary>
/// 404 hit aggregate kaydı — `NotFoundLoggingMiddleware` (Faz 7.3.2) yakalanan
/// her 404 isteğini bu tabloya yazar. Aynı URL'e tekrar isteklere yeni satır
/// AÇMAZ: `HitCount` artırılır, `LastSeenAt` güncellenir.
///
/// BaseEntity DEĞİL — AuditLog pattern: soft-delete YOK (log silinmemeli),
/// Guid Id (uygulama tarafında üretilir), audit ignore listesinde.
///
/// 7.3 sınırı: redirect referans alanı YOK. Faz 7.4 Redirect modülü 404 → 301
/// akışını entegre ederken kendi tablosunda eşler.
/// </summary>
public class NotFoundLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// İstenen path + query string (örn. "/tr-TR/eski-sayfa?utm=x"). Aggregate
    /// unique key. NVARCHAR(850) — SQL Server non-clustered index key sınırı
    /// (1700 byte / 2 byte/char). 850'den uzun URL'ler middleware'de truncate
    /// edilir (Faz 7.3.2 sorumluluğu).
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>İlk hit'in Referer header'ı. Sonraki hit'lerde dokunulmaz.</summary>
    public string? Referer { get; set; }

    /// <summary>İlk hit'in User-Agent header'ı. Sonraki hit'lerde dokunulmaz.</summary>
    public string? UserAgent { get; set; }

    /// <summary>İlk hit'in IP adresi (IPv4/IPv6, max 45 char). Sonraki hit'lerde dokunulmaz.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Toplam hit sayısı. INSERT'te 1, her UPDATE'te +1.</summary>
    public int HitCount { get; set; } = 1;

    /// <summary>İlk 404 hit zamanı. INSERT'te set, sonra DOKUNULMAZ.</summary>
    public DateTime FirstSeenAt { get; set; }

    /// <summary>Son 404 hit zamanı. Her UPDATE'te güncellenir.</summary>
    public DateTime LastSeenAt { get; set; }
}

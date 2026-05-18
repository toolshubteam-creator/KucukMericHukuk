namespace KucukMericHukuk.Core.Entities;

/// <summary>
/// Manuel URL yönlendirme kaydı (Faz 7.4.1). Admin CRUD ile yönetilir;
/// `RedirectMiddleware` (Faz 7.4.2) FromPath lookup yapar ve 301/302 döndürür.
///
/// BaseEntity DEĞİL — soft-delete yerine `IsActive` toggle yeterli, audit'e DÜŞER
/// (admin CRUD denetlenebilir; `SlugHistory` farklı: sistem üretir, audit ignore).
///
/// `HitCount`/`LastHitAt` `RedirectRepository.RecordHitAsync` atomik
/// ExecuteUpdate ile artırılır — interceptor görmez, audit gürültüsü yaratmaz.
/// </summary>
public class Redirect
{
    public int Id { get; set; }

    /// <summary>
    /// Kullanıcının ziyaret ettiği eski URL (path + query, normalize). 500 char
    /// — SQL Server non-clustered index key (1700 byte) içinde rahat sınır.
    /// UNIQUE: aynı FromPath için tek aktif/inaktif kayıt olabilir.
    /// </summary>
    public string FromPath { get; set; } = string.Empty;

    /// <summary>Yeni hedef URL (path veya tam URL).</summary>
    public string ToPath { get; set; } = string.Empty;

    /// <summary>301 (kalıcı, default) veya 302 (geçici).</summary>
    public int StatusCode { get; set; } = 301;

    /// <summary>Pasifleştirilmiş redirect'ler middleware tarafından atlanır.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Atomic ExecuteUpdate ile artırılır (middleware hit'inde).</summary>
    public int HitCount { get; set; }

    /// <summary>Son hit zamanı, hit ile birlikte set'lenir.</summary>
    public DateTime? LastHitAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

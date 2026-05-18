namespace KucukMericHukuk.Core.DTOs.NotFoundLog;

/// <summary>
/// Admin liste sorgusu (Faz 7.3.2). Sıralama default: HitCount DESC, ardından LastSeenAt DESC.
/// </summary>
public class NotFoundLogQueryDto
{
    /// <summary>Url substring araması.</summary>
    public string? Keyword { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;

    /// <summary>LastSeenAt &gt;= StartDate (UTC, inclusive, gün başı).</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>LastSeenAt &lt; EndDate + 1 day (UTC, exclusive).</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Sıralama: "hits" (default — HitCount DESC) veya "recent" (LastSeenAt DESC).
    /// Geçersiz değer "hits" varsayılır.
    /// </summary>
    public string? Sort { get; set; }
}

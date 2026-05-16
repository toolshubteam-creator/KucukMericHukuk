using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Subscriber;

public class SubscriberQueryDto
{
    public string? Keyword { get; set; }

    /// <summary>Null → tüm statüler. Active veya Unsubscribed filtresi.</summary>
    public SubscriberStatus? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>Tarih aralığı filter — SubscribedAt &gt;= StartDate (UTC). Inclusive.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Tarih aralığı filter — SubscribedAt &lt; EndDate + 1 day (UTC). Repository [start, end+1d) exclusive uygular.</summary>
    public DateTime? EndDate { get; set; }
}

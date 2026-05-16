using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.Entities;

public class NewsletterJob : BaseEntity
{
    public int ArticleId { get; set; }
    public Article? Article { get; set; }

    public NewsletterJobStatus Status { get; set; } = NewsletterJobStatus.Pending;

    /// <summary>Gönderim anında set edilir (snapshot — abone listesi anlık değişebilir).</summary>
    public int TotalRecipients { get; set; }

    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Failed durumunda son hata özeti — debug ve admin görünürlüğü için.</summary>
    public string? ErrorSummary { get; set; }
}

using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Newsletter;

public class NewsletterJobListDto
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    public NewsletterJobStatus Status { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Faz 7.2b-2: Failed job için ilk N abone hatası özeti — UI tooltip/details.</summary>
    public string? ErrorSummary { get; set; }

    /// <summary>
    /// Faz 7.2b-2-fix: Şu an aktif (Status=Active) abone sayısı — canlı snapshot.
    /// Confirm modalında "şu kadar aboneye gidecek" mesajı için kullanılır.
    /// `TotalRecipients` ProcessJobAsync Sending'de set edildiği için Pending'de 0 — bu alan
    /// Pending job confirm'inde gerçek hedef sayıyı gösterir. Tüm liste satırlarına aynı snapshot atanır.
    /// </summary>
    public int LiveActiveSubscriberCount { get; set; }
}

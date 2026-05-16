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
}

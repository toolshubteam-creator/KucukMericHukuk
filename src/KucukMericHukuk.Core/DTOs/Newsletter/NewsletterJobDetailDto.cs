using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Newsletter;

public class NewsletterJobDetailDto
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
    public string? ErrorSummary { get; set; }
    public DateTime CreatedAt { get; set; }
}

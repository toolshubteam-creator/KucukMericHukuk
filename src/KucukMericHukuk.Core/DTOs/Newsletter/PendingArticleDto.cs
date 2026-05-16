namespace KucukMericHukuk.Core.DTOs.Newsletter;

public class PendingArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public DateTime? PublishedAt { get; set; }

    /// <summary>Snapshot anki aktif (Status=Active + IsDeleted=false) abone sayısı — gönderim potansiyeli.</summary>
    public int ActiveSubscriberCount { get; set; }
}

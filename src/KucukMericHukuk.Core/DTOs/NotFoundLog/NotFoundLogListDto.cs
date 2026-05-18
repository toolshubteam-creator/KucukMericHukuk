namespace KucukMericHukuk.Core.DTOs.NotFoundLog;

/// <summary>
/// Admin "404 Takibi" liste sayfası için (Faz 7.3.2). Aggregate satır = bir URL.
/// </summary>
public class NotFoundLogListDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Referer { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public int HitCount { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}

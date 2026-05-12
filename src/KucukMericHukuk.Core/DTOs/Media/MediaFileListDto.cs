namespace KucukMericHukuk.Core.DTOs.Media;

public class MediaFileListDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? AltText { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

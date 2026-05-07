namespace KucukMericHukuk.Core.DTOs.Media;

public class MediaUploadInputDto
{
    public Stream FileStream { get; set; } = Stream.Null;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? AltText { get; set; }
    public int? UploadedByUserId { get; set; }
}

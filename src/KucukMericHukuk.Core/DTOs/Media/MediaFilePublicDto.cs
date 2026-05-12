namespace KucukMericHukuk.Core.DTOs.Media;

/// <summary>Public /Galeri sayfası için lightweight DTO. Storage URL'leri service tarafından doldurulur.</summary>
public class MediaFilePublicDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? AltText { get; set; }
    public DateTime CreatedAt { get; set; }
}

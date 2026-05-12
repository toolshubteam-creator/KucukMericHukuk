using KucukMericHukuk.Core.Entities.Identity;

namespace KucukMericHukuk.Core.Entities;

public class MediaFile : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ThumbnailRelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string? AltText { get; set; }

    /// <summary>Public /Galeri sayfasında listelensin mi? Default false (opt-in, KVKK/TBB güvenli).</summary>
    public bool IsPublic { get; set; } = false;

    public int? UploadedByUserId { get; set; }
    public ApplicationUser? UploadedBy { get; set; }
}

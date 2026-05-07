namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Medias;

public class MediaUploadResponseViewModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? Id { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? OriginalFileName { get; set; }
    public string? AltText { get; set; }
}

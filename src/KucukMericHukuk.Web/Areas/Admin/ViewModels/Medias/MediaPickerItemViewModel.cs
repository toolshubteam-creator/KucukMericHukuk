namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Medias;

public class MediaPickerItemViewModel
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class MediaPickerListResponse
{
    public List<MediaPickerItemViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
}

using KucukMericHukuk.Core.DTOs.Media;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Medias;

public class MediaListViewModel
{
    public MediaQuery Query { get; set; } = new();
    public IReadOnlyList<MediaFileListDto> Items { get; set; } = Array.Empty<MediaFileListDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

public class MediaQuery
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public bool IncludeDeleted { get; set; }
}

using KucukMericHukuk.Core.DTOs.Tag;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Tags;

public class TagListViewModel
{
    public TagQueryDto Query { get; set; } = new();
    public IReadOnlyList<TagAdminDto> Items { get; set; } = Array.Empty<TagAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

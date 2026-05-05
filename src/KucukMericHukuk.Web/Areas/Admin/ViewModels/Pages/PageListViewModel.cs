using KucukMericHukuk.Core.DTOs.Page;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Pages;

public class PageListViewModel
{
    public PageQueryDto Query { get; set; } = new();
    public IReadOnlyList<PageAdminDto> Items { get; set; } = Array.Empty<PageAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

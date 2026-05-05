using KucukMericHukuk.Core.DTOs.Category;
using KucukMericHukuk.Core.DTOs.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Categories;

public class CategoryListViewModel
{
    public CategoryQueryDto Query { get; set; } = new();
    public IReadOnlyList<CategoryAdminDto> Items { get; set; } = Array.Empty<CategoryAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    [BindNever]
    public List<LookupDto> AvailableParents { get; set; } = new();
}

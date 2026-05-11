using KucukMericHukuk.Core.DTOs.Faq;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Faqs;

public class FaqListViewModel
{
    public FaqQueryDto Query { get; set; } = new();
    public IReadOnlyList<FaqAdminDto> Items { get; set; } = Array.Empty<FaqAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

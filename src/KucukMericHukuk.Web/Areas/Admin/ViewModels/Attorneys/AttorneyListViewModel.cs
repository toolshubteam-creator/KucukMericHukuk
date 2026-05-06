using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Attorneys;

public class AttorneyListViewModel
{
    public AttorneyQueryDto Query { get; set; } = new();
    public IReadOnlyList<AttorneyAdminDto> Items { get; set; } = Array.Empty<AttorneyAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    [BindNever]
    public List<LookupDto> AvailableServices { get; set; } = new();
}

using KucukMericHukuk.Core.DTOs.Service;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Services;

public class ServiceListViewModel
{
    public ServiceQueryDto Query { get; set; } = new();
    public IReadOnlyList<ServiceAdminDto> Items { get; set; } = Array.Empty<ServiceAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

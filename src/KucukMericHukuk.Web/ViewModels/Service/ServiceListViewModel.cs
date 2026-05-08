using KucukMericHukuk.Core.DTOs.Service;

namespace KucukMericHukuk.Web.ViewModels.Service;

public class ServiceListViewModel
{
    public IReadOnlyList<ServiceListDto> Services { get; init; } = [];
}

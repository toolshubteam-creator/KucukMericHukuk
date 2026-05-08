using KucukMericHukuk.Core.DTOs.Service;

namespace KucukMericHukuk.Web.ViewModels.Service;

public class ServiceDetailViewModel
{
    public ServiceDetailDto Service { get; init; } = new();
    public IReadOnlyList<ServiceListDto> OtherServices { get; init; } = [];
}

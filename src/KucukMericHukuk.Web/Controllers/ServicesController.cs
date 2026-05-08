using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Service;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class ServicesController : Controller
{
    private const int OtherServicesCount = 3;

    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    [HttpGet]
    [Route("{culture:culture}/Services", Order = 0)]
    [Route("{culture:culture}/Services/Index", Order = 1)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;
        var services = await _serviceService.GetActiveOrderedAsync(lang, take: null, ct);

        var vm = new ServiceListViewModel { Services = services };
        return View(vm);
    }

    [HttpGet]
    [Route("{culture:culture}/Services/{slug:regex(^(?!Index$).+)}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;

        var result = await _serviceService.GetBySlugAsync(slug, lang, ct);
        if (result.IsFailure)
        {
            return NotFound();
        }

        var service = result.Value;

        var allActive = await _serviceService.GetActiveOrderedAsync(lang, take: null, ct);
        var others = allActive.Where(s => s.Id != service.Id).Take(OtherServicesCount).ToList();

        var vm = new ServiceDetailViewModel
        {
            Service = service,
            OtherServices = others
        };
        return View(vm);
    }
}

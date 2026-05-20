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
    [Route("{culture:trCulture}/hizmetler", Order = 0)]
    [Route("{culture:enCulture}/services", Order = 0)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;
        var services = await _serviceService.GetActiveOrderedAsync(lang, take: null, ct);

        var vm = new ServiceListViewModel { Services = services };
        return View(vm);
    }

    [HttpGet]
    [Route("{culture:trCulture}/hizmetler/{slug:regex(^(?!Index$).+)}", Order = 0)]
    [Route("{culture:enCulture}/services/{slug:regex(^(?!Index$).+)}", Order = 0)]
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

    [HttpGet]
    [Route("{culture:trCulture}/Services", Order = 1)]
    [Route("{culture:trCulture}/Services/Index", Order = 2)]
    public IActionResult LegacyIndex(string culture)
    {
        return RedirectToActionPermanent(nameof(Index), new { culture });
    }

    [HttpGet]
    [Route("{culture:trCulture}/Services/{slug:regex(^(?!Index$).+)}", Order = 1)]
    public IActionResult LegacyDetail(string culture, string slug)
    {
        return RedirectToActionPermanent(nameof(Detail), new { culture, slug });
    }
}

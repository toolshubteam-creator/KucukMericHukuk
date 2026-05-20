using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Faq;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class FaqsController : Controller
{
    private readonly IFaqService _faqService;

    public FaqsController(IFaqService faqService)
    {
        _faqService = faqService;
    }

    [HttpGet]
    [Route("{culture:trCulture}/sss", Order = 0)]
    [Route("{culture:enCulture}/faq", Order = 0)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;
        var faqs = await _faqService.GetActiveOrderedAsync(lang, ct);

        var vm = new FaqListViewModel { Faqs = faqs };
        return View(vm);
    }

    [HttpGet]
    [Route("{culture:trCulture}/Faqs", Order = 1)]
    public IActionResult LegacyIndex(string culture)
    {
        return RedirectToActionPermanent(nameof(Index), new { culture });
    }
}

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
    [Route("{culture:culture}/Faqs")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;
        var faqs = await _faqService.GetActiveOrderedAsync(lang, ct);

        var vm = new FaqListViewModel { Faqs = faqs };
        return View(vm);
    }
}

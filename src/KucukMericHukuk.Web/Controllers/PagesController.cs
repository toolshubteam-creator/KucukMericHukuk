using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Page;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class PagesController : Controller
{
    private readonly IPageService _pageService;

    public PagesController(IPageService pageService)
    {
        _pageService = pageService;
    }

    [HttpGet]
    [Route("{culture:culture}/Pages/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;

        var result = await _pageService.GetBySlugAsync(slug, lang, ct);
        if (result.IsFailure || !result.Value.IsActive)
        {
            return NotFound();
        }

        var vm = new PageDetailViewModel { Page = result.Value };
        return View(vm);
    }
}

using System.Diagnostics;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Models;
using KucukMericHukuk.Web.Resources;
using KucukMericHukuk.Web.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KucukMericHukuk.Web.Controllers;

public class HomeController : Controller
{
    private readonly IServiceService _serviceService;
    private readonly IAttorneyService _attorneyService;
    private readonly IArticleService _articleService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public HomeController(
        IServiceService serviceService,
        IAttorneyService attorneyService,
        IArticleService articleService,
        IStringLocalizer<SharedResource> localizer)
    {
        _serviceService = serviceService;
        _attorneyService = attorneyService;
        _articleService = articleService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;

        var services = await _serviceService.GetActiveOrderedAsync(lang, take: 3, ct);
        var attorneys = await _attorneyService.GetActiveOrderedAsync(lang, take: 3, ct);
        var articles = await _articleService.GetFeaturedOrRecentAsync(lang, count: 3, ct);

        var vm = new HomeIndexViewModel
        {
            FeaturedServices = services,
            FeaturedAttorneys = attorneys,
            RecentArticles = articles
        };

        ViewData["Title"] = _localizer["HomeTitle"];
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

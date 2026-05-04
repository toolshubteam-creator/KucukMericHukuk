using System.Diagnostics;
using KucukMericHukuk.Web.Models;
using KucukMericHukuk.Web.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KucukMericHukuk.Web.Controllers;

public class HomeController : Controller
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public HomeController(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = _localizer["HomeTitle"];
        ViewData["Welcome"] = _localizer["WelcomeMessage"];
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

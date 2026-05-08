using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class ArticlesController : Controller
{
    [HttpGet]
    public IActionResult Detail(string slug)
    {
        ViewData["Slug"] = slug;
        return View();
    }
}

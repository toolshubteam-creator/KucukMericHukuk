using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class ServicesController : Controller
{
    [HttpGet]
    public IActionResult Detail(string slug)
    {
        ViewData["Slug"] = slug;
        return View();
    }
}

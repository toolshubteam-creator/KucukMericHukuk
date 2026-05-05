using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Kontrol Paneli";
        return View();
    }
}

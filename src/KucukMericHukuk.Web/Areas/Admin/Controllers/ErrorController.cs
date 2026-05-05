using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/error")]
[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("401")]
    public IActionResult Unauthorized401()
    {
        ViewData["Title"] = "Yetkisiz Erişim";
        return View("Unauthorized");
    }

    [Route("403")]
    [Route("access-denied")]
    public IActionResult Forbidden403()
    {
        ViewData["Title"] = "Erişim Reddedildi";
        return View("AccessDenied");
    }

    [Route("404")]
    public IActionResult NotFound404()
    {
        ViewData["Title"] = "Sayfa Bulunamadı";
        return View("NotFound");
    }
}

using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class ErrorController : Controller
{
    // Middleware UseStatusCodePagesWithReExecute("/tr-TR/Error/{0}") buraya yönlendirir.
    // ReExecute orijinal HTTP method'u (POST/GET/...) korur — AcceptVerbs hepsini kabul eder.
    [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH")]
    [Route("{culture:culture}/Error/{code:int}")]
    public IActionResult HandleStatusCode(int code)
    {
        // Direct hit (middleware re-execute değilse) için status code'u set et.
        // ReExecute durumunda middleware orijinal kodu zaten restore eder, bu set ezilir — sorun değil.
        Response.StatusCode = code;
        if (code >= 500) return View("Server");
        return View("NotFound");
    }

    // Culture-less fallback (middleware redirect culture eklemeden gelirse).
    [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH")]
    [Route("Error/{code:int}")]
    public IActionResult HandleStatusCodeNoCulture(int code)
    {
        Response.StatusCode = code;
        if (code >= 500) return View("Server");
        return View("NotFound");
    }
}

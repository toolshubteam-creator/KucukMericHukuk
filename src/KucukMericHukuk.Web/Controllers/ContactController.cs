using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Contact;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KucukMericHukuk.Web.Controllers;

public class ContactController : Controller
{
    private readonly IContactMessageService _contactService;
    private readonly ITurnstileVerifier _turnstile;

    public ContactController(IContactMessageService contactService, ITurnstileVerifier turnstile)
    {
        _contactService = contactService;
        _turnstile = turnstile;
    }

    [HttpGet]
    [Route("{culture:culture}/Contact", Order = 0)]
    [Route("{culture:culture}/Contact/Index", Order = 1)]
    public IActionResult Index()
    {
        return View(new ContactFormViewModel());
    }

    [HttpPost]
    [Route("{culture:culture}/Contact/Submit")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("contact-form")]
    public async Task<IActionResult> Submit(ContactFormViewModel vm, CancellationToken ct)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;

        // Turnstile bot doğrulama (honeypot ikinci katman, service-level)
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var turnstileOk = await _turnstile.VerifyAsync(vm.TurnstileToken, remoteIp, ct);
        if (!turnstileOk)
        {
            ModelState.AddModelError(string.Empty,
                "Bot doğrulaması başarısız oldu. Lütfen sayfayı yenileyip tekrar deneyin.");
            return View("Index", vm);
        }

        var dto = new ContactFormDto
        {
            Name = vm.Name ?? "",
            Email = vm.Email ?? "",
            Phone = vm.Phone,
            Subject = vm.Subject ?? "",
            Message = vm.Message ?? "",
            KvkkConsent = vm.KvkkConsent,
            Website = vm.Website,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        var result = await _contactService.SaveAsync(dto, ct);
        if (result.IsFailure)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Message);
            }
            return View("Index", vm);
        }

        return RedirectToAction(nameof(ThankYou), new { culture = lang });
    }

    [HttpGet]
    [Route("{culture:culture}/Contact/ThankYou")]
    public IActionResult ThankYou()
    {
        return View();
    }
}

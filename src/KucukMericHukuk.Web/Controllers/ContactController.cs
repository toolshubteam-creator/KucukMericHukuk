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
    [Route("{culture:trCulture}/iletisim", Order = 0)]
    [Route("{culture:enCulture}/contact", Order = 0)]
    public IActionResult Index()
    {
        return View(new ContactFormViewModel());
    }

    [HttpPost]
    [Route("{culture:trCulture}/iletisim/gonder", Order = 0)]
    [Route("{culture:enCulture}/contact/submit", Order = 0)]
    [Route("{culture:trCulture}/Contact/Submit", Order = 1)]
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
    [Route("{culture:trCulture}/iletisim/tesekkurler", Order = 0)]
    [Route("{culture:enCulture}/contact/thank-you", Order = 0)]
    public IActionResult ThankYou()
    {
        return View();
    }

    [HttpGet]
    [Route("{culture:trCulture}/Contact", Order = 1)]
    [Route("{culture:trCulture}/Contact/Index", Order = 2)]
    public IActionResult LegacyIndex(string culture)
    {
        return RedirectToActionPermanent(nameof(Index), new { culture });
    }

    [HttpGet]
    [Route("{culture:trCulture}/Contact/ThankYou", Order = 1)]
    public IActionResult LegacyThankYou(string culture)
    {
        return RedirectToActionPermanent(nameof(ThankYou), new { culture });
    }
}

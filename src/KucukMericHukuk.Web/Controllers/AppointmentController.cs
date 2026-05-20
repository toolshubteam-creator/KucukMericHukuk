using KucukMericHukuk.Core.DTOs.Appointment;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Appointment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KucukMericHukuk.Web.Controllers;

public class AppointmentController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly ITurnstileVerifier _turnstile;

    public AppointmentController(IAppointmentService appointmentService, ITurnstileVerifier turnstile)
    {
        _appointmentService = appointmentService;
        _turnstile = turnstile;
    }

    [HttpGet]
    [Route("{culture:trCulture}/randevu", Order = 0)]
    [Route("{culture:enCulture}/appointment", Order = 0)]
    public IActionResult Index()
    {
        return View(new AppointmentFormViewModel());
    }

    [HttpPost]
    [Route("{culture:trCulture}/randevu/gonder", Order = 0)]
    [Route("{culture:enCulture}/appointment/submit", Order = 0)]
    [Route("{culture:trCulture}/Appointment/Submit", Order = 1)]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("appointment-form")]
    public async Task<IActionResult> Submit(AppointmentFormViewModel vm, CancellationToken ct)
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

        var dto = new AppointmentFormDto
        {
            Name = vm.Name ?? "",
            Email = vm.Email ?? "",
            Phone = vm.Phone ?? "",
            Subject = vm.Subject ?? "",
            PreferredDate = vm.PreferredDate,
            PreferredTimeNote = vm.PreferredTimeNote,
            Notes = vm.Notes,
            KvkkConsent = vm.KvkkConsent,
            Website = vm.Website,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        var result = await _appointmentService.SaveAsync(dto, ct);
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
    [Route("{culture:trCulture}/randevu/tesekkurler", Order = 0)]
    [Route("{culture:enCulture}/appointment/thank-you", Order = 0)]
    public IActionResult ThankYou()
    {
        return View();
    }

    [HttpGet]
    [Route("{culture:trCulture}/Appointment", Order = 1)]
    [Route("{culture:trCulture}/Appointment/Index", Order = 2)]
    public IActionResult LegacyIndex(string culture)
    {
        return RedirectToActionPermanent(nameof(Index), new { culture });
    }

    [HttpGet]
    [Route("{culture:trCulture}/Appointment/ThankYou", Order = 1)]
    public IActionResult LegacyThankYou(string culture)
    {
        return RedirectToActionPermanent(nameof(ThankYou), new { culture });
    }
}

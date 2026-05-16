using KucukMericHukuk.Core.DTOs.Subscriber;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Subscriber;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KucukMericHukuk.Web.Controllers;

public class SubscriberController : Controller
{
    private readonly ISubscriberService _subscriberService;
    private readonly ITurnstileVerifier _turnstile;

    public SubscriberController(ISubscriberService subscriberService, ITurnstileVerifier turnstile)
    {
        _subscriberService = subscriberService;
        _turnstile = turnstile;
    }

    [HttpPost]
    [Route("{culture:culture}/Subscriber/Subscribe")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("subscribe-form")]
    public async Task<IActionResult> Subscribe(SubscriberFormViewModel vm, string? returnUrl, CancellationToken ct)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var turnstileOk = await _turnstile.VerifyAsync(vm.TurnstileToken, remoteIp, ct);
        if (!turnstileOk)
        {
            TempData["SubscribeError"] = "Bot doğrulaması başarısız oldu. Lütfen sayfayı yenileyip tekrar deneyin.";
            return RedirectToSafeReturn(returnUrl, culture);
        }

        var dto = new SubscriberFormDto
        {
            Email = vm.Email ?? string.Empty,
            KvkkConsent = vm.KvkkConsent,
            Website = vm.Website,
            IpAddress = remoteIp,
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        var result = await _subscriberService.SubscribeAsync(dto, ct);
        if (result.IsFailure)
        {
            TempData["SubscribeError"] = result.FirstError?.Message ?? "Abonelik kaydı yapılamadı.";
            return RedirectToSafeReturn(returnUrl, culture);
        }

        TempData["SubscribeSuccess"] = "Aboneliğiniz alındı. Teşekkür ederiz.";
        return RedirectToSafeReturn(returnUrl, culture);
    }

    [HttpGet]
    [Route("{culture:culture}/Subscriber/Unsubscribe/{token:guid}")]
    public async Task<IActionResult> Unsubscribe(Guid token, CancellationToken ct)
    {
        var result = await _subscriberService.UnsubscribeByTokenAsync(token, ct);
        ViewData["UnsubscribeSucceeded"] = result.IsSuccess;
        ViewData["UnsubscribeMessage"] = result.IsSuccess
            ? "Aboneliğiniz iptal edildi."
            : result.FirstError?.Message ?? "Geçersiz iptal bağlantısı.";
        return View();
    }

    private IActionResult RedirectToSafeReturn(string? returnUrl, string culture)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home", new { culture });
    }
}

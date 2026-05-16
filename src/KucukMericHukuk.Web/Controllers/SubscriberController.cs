using KucukMericHukuk.Core.DTOs.Subscriber;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Subscriber;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KucukMericHukuk.Web.Controllers;

public class SubscriberController : Controller
{
    private const string TurnstileFailedMessage =
        "Bot doğrulaması başarısız oldu. Lütfen sayfayı yenileyip tekrar deneyin.";
    private const string SubscribeFailedMessage = "Abonelik kaydı yapılamadı.";
    private const string SubscribeSuccessMessage = "Aboneliğiniz alındı. Teşekkür ederiz.";

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
        var wantsJson = WantsJsonResponse();

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var turnstileOk = await _turnstile.VerifyAsync(vm.TurnstileToken, remoteIp, ct);
        if (!turnstileOk)
        {
            return Respond(wantsJson, success: false, message: TurnstileFailedMessage, returnUrl, culture);
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
            var message = result.FirstError?.Message ?? SubscribeFailedMessage;
            return Respond(wantsJson, success: false, message, returnUrl, culture);
        }

        return Respond(wantsJson, success: true, message: SubscribeSuccessMessage, returnUrl, culture);
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

    private bool WantsJsonResponse()
    {
        // Faz 7.2a-fix: AJAX submit Accept: application/json gönderir → JSON dön.
        // Header yoksa veya text/html önceliğindeyse fallback POST-redirect (JS-disabled progressive enhancement).
        var accept = Request.Headers["Accept"].ToString();
        return !string.IsNullOrEmpty(accept) && accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult Respond(bool wantsJson, bool success, string message, string? returnUrl, string culture)
    {
        if (wantsJson)
        {
            return Json(new { success, message });
        }

        if (success)
        {
            TempData["SubscribeSuccess"] = message;
        }
        else
        {
            TempData["SubscribeError"] = message;
        }
        return RedirectToSafeReturn(returnUrl, culture);
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

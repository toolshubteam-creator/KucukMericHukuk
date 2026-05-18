using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.NotFoundLog;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/notfound-logs")]
[Authorize(Roles = "Admin")]
public class NotFoundLogsController : Controller
{
    private readonly INotFoundService _service;
    private readonly IRedirectService _redirectService;
    private readonly ILogger<NotFoundLogsController> _logger;

    public NotFoundLogsController(
        INotFoundService service,
        IRedirectService redirectService,
        ILogger<NotFoundLogsController> logger)
    {
        _service = service;
        _redirectService = redirectService;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(NotFoundLogQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "404 Kayıtları";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 30;

        var result = await _service.GetPagedAsync(query, ct);
        if (result.IsFailure)
        {
            _logger.LogError("404 listesi alinamadi: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "404 kayıtları yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Query = query;
        return View(result.Value);
    }

    [HttpPost("purge/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeOne(Guid id, CancellationToken ct)
    {
        var result = await _service.PurgeAsync(id, ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.FirstError?.Message ?? "Kayıt silinemedi.";
        }
        else
        {
            TempData["Success"] = "Kayıt silindi.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("purge-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeAll(CancellationToken ct)
    {
        var result = await _service.PurgeAllAsync(ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.FirstError?.Message ?? "Kayıtlar silinemedi.";
        }
        else
        {
            TempData["Success"] = $"{result.Value} kayıt silindi.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Faz 7.4.3b — 404 kaydından tek-tık redirect kur. FromPath otomatik
    /// (NotFoundLog.Url), ToPath admin formdan, StatusCode 301 default veya 302.
    /// Akış: NotFoundLog bul → RedirectService.CreateAsync (duplicate/cycle/self
    /// reddi otomatik) → başarılı ise NotFoundLog satırını sil (artık çözüldü).
    /// Başarısızsa NotFoundLog korunur (admin tekrar deneyebilir).
    /// </summary>
    [HttpPost("create-redirect/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRedirect(
        Guid id, string toPath, int statusCode = 301, CancellationToken ct = default)
    {
        var notFound = await _service.GetByIdAsync(id, ct);
        if (notFound.IsFailure)
        {
            TempData["Error"] = "404 kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var createResult = await _redirectService.CreateAsync(new RedirectFormDto
        {
            FromPath = notFound.Value!.Url,
            ToPath = toPath ?? string.Empty,
            StatusCode = statusCode,
            IsActive = true
        }, ct);

        if (createResult.IsFailure)
        {
            // Duplicate / Self / Cycle / Validation → service mesajını göster, NotFoundLog korunur.
            TempData["Error"] = createResult.FirstError?.Message ?? "Yönlendirme oluşturulamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Redirect başarılı → NotFoundLog kaydı "çözüldü" sayılır, listeyi temiz tut.
        var purge = await _service.PurgeAsync(id, ct);
        if (purge.IsFailure)
        {
            // Redirect var, 404 kaldı — kullanıcıya bilgi, kritik değil (manuel temizlenebilir).
            _logger.LogWarning(
                "CreateRedirect: redirect olusturuldu (Id={RedirectId}) ama NotFoundLog purge basarisiz (NotFoundId={NotFoundId})",
                createResult.Value, id);
        }

        TempData["Success"] = $"Yönlendirme oluşturuldu: {notFound.Value.Url} → {toPath}";
        return RedirectToAction(nameof(Index));
    }
}

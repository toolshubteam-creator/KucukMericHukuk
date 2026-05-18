using KucukMericHukuk.Core.DTOs.NotFoundLog;
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
    private readonly ILogger<NotFoundLogsController> _logger;

    public NotFoundLogsController(
        INotFoundService service,
        ILogger<NotFoundLogsController> logger)
    {
        _service = service;
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
}

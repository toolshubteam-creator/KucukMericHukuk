using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Subscriber;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/subscribers")]
[Authorize(Roles = "Admin")]
public class SubscribersController : Controller
{
    private readonly ISubscriberService _service;
    private readonly ILogger<SubscribersController> _logger;

    public SubscribersController(ISubscriberService service, ILogger<SubscribersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(SubscriberQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Aboneler";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;

        var result = await _service.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Abone listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Abone listesi yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Query = query;
        return View(result.Value);
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Subscriber.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message ?? "Abone silinirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Abone silindi (geri alınabilir).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _service.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Subscriber.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message ?? "Abone geri yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Abone geri yüklendi.";
        return RedirectToAction(nameof(Index), new { includeDeleted = true });
    }
}

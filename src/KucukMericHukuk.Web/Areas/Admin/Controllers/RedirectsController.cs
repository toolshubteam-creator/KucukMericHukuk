using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/redirects")]
[Authorize(Roles = "Admin")]
public class RedirectsController : Controller
{
    private readonly IRedirectService _service;
    private readonly ILogger<RedirectsController> _logger;

    public RedirectsController(IRedirectService service, ILogger<RedirectsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(RedirectQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Yönlendirmeler";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 30;

        var result = await _service.GetAdminPagedAsync(query, ct);
        if (result.IsFailure)
        {
            _logger.LogError("Redirect listesi alinamadi: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Yönlendirmeler yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Query = query;
        return View(result.Value);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Yeni Yönlendirme";
        return View(new RedirectFormDto { StatusCode = 301, IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RedirectFormDto input, CancellationToken ct)
    {
        var result = await _service.CreateAsync(input, ct);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(input);
        }
        TempData["Success"] = "Yönlendirme oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Yönlendirme Düzenle";
        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Redirect.NotFound) return NotFound();
            TempData["Error"] = "Yönlendirme yüklenemedi.";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Value!;
        return View(new RedirectFormDto
        {
            Id = dto.Id,
            FromPath = dto.FromPath,
            ToPath = dto.ToPath ?? string.Empty,
            StatusCode = dto.StatusCode,
            IsActive = dto.IsActive
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RedirectFormDto input, CancellationToken ct)
    {
        input.Id = id;
        var result = await _service.UpdateAsync(input, ct);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(input);
        }
        TempData["Success"] = "Yönlendirme güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.FirstError?.Message ?? "Silinemedi.";
        }
        else
        {
            TempData["Success"] = "Yönlendirme silindi.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("toggle-active/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        var result = await _service.ToggleActiveAsync(id, ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.FirstError?.Message ?? "İşlem başarısız.";
        }
        else
        {
            TempData["Success"] = "Yönlendirme durumu güncellendi.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Faz 7.4.3a — admin formdan AJAX loop-check (ToPath blur sonrası). JSON dönecek.
    /// </summary>
    [HttpGet("check-cycle")]
    public async Task<IActionResult> CheckCycle(
        string from, string to, int? excludeId = null, CancellationToken ct = default)
    {
        var result = await _service.CheckCycleAsync(from ?? string.Empty, to ?? string.Empty, excludeId, ct);
        return Json(new { ok = result.Ok, message = result.Message });
    }
}

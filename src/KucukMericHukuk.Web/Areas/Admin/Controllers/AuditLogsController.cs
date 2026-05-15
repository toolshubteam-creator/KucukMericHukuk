using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.AuditLog;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _service;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditLogService service, ILogger<AuditLogsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(AuditLogQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Aktivite Logu";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 30;

        var result = await _service.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Audit log listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Aktivite logu yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Query = query;
        return View(result.Value);
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Aktivite Kaydı Detayı";

        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.AuditLog.NotFound)
                return NotFound();

            TempData["Error"] = "Audit kaydı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }
}

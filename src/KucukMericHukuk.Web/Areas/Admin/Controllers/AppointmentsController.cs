using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Appointment;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/appointments")]
[Authorize(Roles = "Admin")]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _service;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService service,
        ILogger<AppointmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(AppointmentQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Randevu Talepleri";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;

        var result = await _service.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Randevu talebi listesi alinamadi: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Randevu talepleri yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Query = query;
        return View(result.Value);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Randevu Talebi Detayı";

        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Appointment.NotFound)
                return NotFound();

            TempData["Error"] = "Randevu talebi yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpPost("confirm/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, string? adminNote, CancellationToken ct)
    {
        var result = await _service.ConfirmAsync(id, adminNote, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Appointment.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message
                ?? "Randevu onaylanırken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Randevu onaylandı, müvekkile bilgilendirme e-postası gönderildi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("reject/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? adminNote, CancellationToken ct)
    {
        var result = await _service.RejectAsync(id, adminNote, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Appointment.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message
                ?? "Randevu reddedilirken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Randevu reddedildi, müvekkile bilgilendirme e-postası gönderildi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("complete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var result = await _service.CompleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Appointment.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message
                ?? "Randevu tamamlandı olarak işaretlenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Randevu tamamlandı olarak işaretlendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Appointment.NotFound)
                return NotFound();

            _logger.LogWarning("Randevu talebi silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Randevu talebi silinirken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Randevu talebi silindi (geri alınabilir).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _service.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Appointment.NotFound)
                return NotFound();

            _logger.LogWarning("Randevu talebi geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Randevu talebi geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Randevu talebi geri yüklendi.";
        return RedirectToAction(nameof(Index), new { includeDeleted = true });
    }
}

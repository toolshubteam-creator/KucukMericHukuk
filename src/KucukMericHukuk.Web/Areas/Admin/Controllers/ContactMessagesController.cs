using System.Security.Claims;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/contact-messages")]
[Authorize(Roles = "Admin,Editor")]
public class ContactMessagesController : Controller
{
    private readonly IContactMessageService _service;
    private readonly ILogger<ContactMessagesController> _logger;

    public ContactMessagesController(
        IContactMessageService service,
        ILogger<ContactMessagesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(ContactMessageQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "İletişim Mesajları";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;

        var result = await _service.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("İletişim mesajı listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "İletişim mesajları yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        ViewBag.Query = query;
        return View(result.Value);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "İletişim Mesajı Detayı";

        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.ContactMessage.NotFound)
                return NotFound();

            TempData["Error"] = "Mesaj yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        // Otomatik okundu işaretle (gmail pattern; idempotent)
        if (!result.Value.IsRead && !result.Value.IsDeleted)
        {
            await _service.MarkAsReadAsync(id, ct);
            result.Value.IsRead = true;
        }

        return View(result.Value);
    }

    [HttpPost("toggle-read/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRead(int id, CancellationToken ct)
    {
        var result = await _service.ToggleReadAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.ContactMessage.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message
                ?? "Okundu durumu güncellenirken bir sorun oluştu.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("toggle-answered/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAnswered(int id, CancellationToken ct)
    {
        var result = await _service.ToggleAnsweredAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.ContactMessage.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message
                ?? "Yanıt durumu güncellenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Yanıt durumu güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.ContactMessage.NotFound)
                return NotFound();

            _logger.LogWarning("Mesaj silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Mesaj silinirken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Mesaj silindi (geri alınabilir).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("reply/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string body, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdClaim, out var parsed) ? parsed : null;

        var input = new ContactMessageReplyInputDto { ContactMessageId = id, Body = body ?? string.Empty };
        var result = await _service.ReplyAsync(input, userId, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.ContactMessage.NotFound)
                return NotFound();

            _logger.LogWarning("Yanıt gönderilemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message ?? "Yanıt gönderilemedi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Yanıt gönderildi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _service.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.ContactMessage.NotFound)
                return NotFound();

            _logger.LogWarning("Mesaj geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Mesaj geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Mesaj geri yüklendi.";
        return RedirectToAction(nameof(Index), new { includeDeleted = true });
    }
}

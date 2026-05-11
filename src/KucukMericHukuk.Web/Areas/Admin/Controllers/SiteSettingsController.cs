using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.SiteSetting;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.SiteSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/site-settings")]
[Authorize(Roles = "Admin")]
public class SiteSettingsController : Controller
{
    private readonly ISiteSettingsService _service;
    private readonly ILogger<SiteSettingsController> _logger;

    public SiteSettingsController(
        ISiteSettingsService service,
        ILogger<SiteSettingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(string? activeGroup, CancellationToken ct)
    {
        ViewData["Title"] = "Site Ayarları";

        var result = await _service.GetGroupedAsync(ct);
        if (result.IsFailure)
        {
            _logger.LogError("SiteSettings yüklenemedi: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Ayarlar yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        var vm = new SiteSettingsViewModel
        {
            Groups = result.Value,
            ActiveGroup = string.IsNullOrWhiteSpace(activeGroup)
                ? (result.Value.FirstOrDefault()?.Group ?? "SiteInfo")
                : activeGroup
        };

        return View(vm);
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SiteSettingUpdateInput input, CancellationToken ct)
    {
        var result = await _service.UpdateGroupAsync(input, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.SiteSetting.GroupNotFound)
                return NotFound();

            _logger.LogWarning("SiteSettings güncelleme hatası: group={Group}, errors={Errors}",
                input.Group, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Message));
            return RedirectToAction(nameof(Index), new { activeGroup = input.Group });
        }

        TempData["Success"] = $"\"{input.Group}\" grubu ayarları kaydedildi.";
        return RedirectToAction(nameof(Index), new { activeGroup = input.Group });
    }
}

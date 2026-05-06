using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Page;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Pages;
using KucukMericHukuk.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/pages")]
[Authorize(Roles = "Admin,Editor")]
public class PagesController : Controller
{
    private readonly IPageService _pageService;
    private readonly ILogger<PagesController> _logger;

    public PagesController(IPageService pageService, ILogger<PagesController> logger)
    {
        _pageService = pageService;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(PageQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Sayfalar";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;
        if (string.IsNullOrWhiteSpace(query.LanguageCode)) query.LanguageCode = "tr-TR";

        var result = await _pageService.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Sayfa listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => $"[{e.Code}] {e.Message}")));
            TempData["Error"] = "Sayfa listesi yüklenirken bir sorun oluştu.";
            return View(new PageListViewModel { Query = query });
        }

        var vm = new PageListViewModel
        {
            Query = query,
            Items = result.Value.Items,
            TotalCount = result.Value.TotalCount,
            PageNumber = result.Value.PageNumber,
            PageSize = result.Value.PageSize,
            TotalPages = result.Value.TotalPages,
            HasPrevious = result.Value.HasPrevious,
            HasNext = result.Value.HasNext,
        };

        return View(vm);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Sayfa Detayı";

        var result = await _pageService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Page.NotFound)
            {
                return NotFound();
            }

            _logger.LogError("Sayfa detayı alınamadı: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Sayfa detayı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Yeni Sayfa";

        var vm = new PageFormViewModel
        {
            IsActive = true,
            DisplayOrder = 0,
            Translations = LanguageCodes.Supported
                .Select(lang => new PageTranslationFormViewModel { LanguageCode = lang })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PageFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Sayfa";

        var input = form.Adapt<PageInputDto>();
        var result = await _pageService.CreateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "Sayfa başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Sayfa Düzenle";

        var result = await _pageService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Page.NotFound)
                return NotFound();
            TempData["Error"] = "Sayfa yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = result.Value.Adapt<PageFormViewModel>();

        var existingLangs = vm.Translations.Select(t => t.LanguageCode).ToHashSet();
        foreach (var lang in LanguageCodes.Supported)
        {
            if (!existingLangs.Contains(lang))
            {
                vm.Translations.Add(new PageTranslationFormViewModel { LanguageCode = lang });
            }
        }

        vm.Translations = vm.Translations
            .OrderBy(t => Array.IndexOf(LanguageCodes.Supported, t.LanguageCode))
            .ToList();

        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PageFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Sayfa Düzenle";

        if (form.Id != id)
        {
            return BadRequest();
        }

        var input = form.Adapt<PageInputDto>();
        input.Id = id;

        var result = await _pageService.UpdateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "Sayfa başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _pageService.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Page.NotFound)
                return NotFound();

            _logger.LogWarning("Sayfa silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Sayfa silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Sayfa silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _pageService.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Page.NotFound)
                return NotFound();

            _logger.LogWarning("Sayfa geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Sayfa geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Sayfa geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var result = await _pageService.HardDeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Page.NotFound)
                return NotFound();

            _logger.LogWarning("Sayfa kalıcı olarak silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Sayfa kalıcı olarak silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Sayfa kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Faqs;
using KucukMericHukuk.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/faqs")]
[Authorize(Roles = "Admin")]
public class FaqsController : Controller
{
    private readonly IFaqService _faqService;
    private readonly ILogger<FaqsController> _logger;

    public FaqsController(IFaqService faqService, ILogger<FaqsController> logger)
    {
        _faqService = faqService;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(FaqQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Sık Sorulan Sorular";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;
        if (string.IsNullOrWhiteSpace(query.LanguageCode)) query.LanguageCode = LanguageCodes.Turkish;

        var result = await _faqService.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("SSS listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "SSS listesi yüklenirken bir sorun oluştu.";
            return View(new FaqListViewModel { Query = query });
        }

        var vm = new FaqListViewModel
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
        ViewData["Title"] = "SSS Detayı";

        var result = await _faqService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Faq.NotFound)
                return NotFound();

            TempData["Error"] = "SSS detayı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Yeni SSS";

        var vm = new FaqFormViewModel
        {
            IsActive = true,
            DisplayOrder = 0,
            Translations = LanguageCodes.Supported
                .Select(lang => new FaqTranslationFormViewModel { LanguageCode = lang })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FaqFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni SSS";

        var input = form.Adapt<FaqInputDto>();
        var result = await _faqService.CreateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "SSS başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "SSS Düzenle";

        var result = await _faqService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Faq.NotFound)
                return NotFound();

            TempData["Error"] = "SSS yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = result.Value.Adapt<FaqFormViewModel>();

        var existingLangs = vm.Translations.Select(t => t.LanguageCode).ToHashSet();
        foreach (var lang in LanguageCodes.Supported)
        {
            if (!existingLangs.Contains(lang))
            {
                vm.Translations.Add(new FaqTranslationFormViewModel { LanguageCode = lang });
            }
        }

        vm.Translations = vm.Translations
            .OrderBy(t => Array.IndexOf(LanguageCodes.Supported, t.LanguageCode))
            .ToList();

        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FaqFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "SSS Düzenle";

        if (form.Id != id)
            return BadRequest();

        var input = form.Adapt<FaqInputDto>();
        input.Id = id;

        var result = await _faqService.UpdateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "SSS başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _faqService.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Faq.NotFound)
                return NotFound();

            _logger.LogWarning("SSS silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "SSS silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "SSS silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _faqService.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Faq.NotFound)
                return NotFound();

            _logger.LogWarning("SSS geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "SSS geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "SSS geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var result = await _faqService.HardDeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Faq.NotFound)
                return NotFound();

            _logger.LogWarning("SSS kalıcı olarak silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "SSS kalıcı olarak silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "SSS kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Drag-drop UI sonrası batch DisplayOrder güncelleme. JSON body POST + RequestVerificationToken header.
    /// </summary>
    [HttpPost("reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(
        [FromBody] List<FaqReorderItemDto> items,
        CancellationToken ct)
    {
        var result = await _faqService.ReorderAsync(items, ct);

        if (result.IsFailure)
        {
            _logger.LogWarning("SSS sıralama güncellenemedi: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            return BadRequest(new
            {
                error = result.FirstError?.Message ?? "Sıralama güncellenemedi."
            });
        }

        return Ok();
    }
}

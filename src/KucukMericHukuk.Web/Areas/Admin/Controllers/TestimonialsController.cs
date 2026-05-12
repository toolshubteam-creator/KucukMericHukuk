using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Testimonial;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Testimonials;
using KucukMericHukuk.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/testimonials")]
[Authorize(Roles = "Admin")]
public class TestimonialsController : Controller
{
    private readonly ITestimonialService _service;
    private readonly ILogger<TestimonialsController> _logger;

    public TestimonialsController(ITestimonialService service, ILogger<TestimonialsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(TestimonialQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Müvekkil Yorumları";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;
        if (string.IsNullOrWhiteSpace(query.LanguageCode)) query.LanguageCode = LanguageCodes.Turkish;

        var result = await _service.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Müvekkil yorumları listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Müvekkil yorumları yüklenirken bir sorun oluştu.";
            return View(new TestimonialListViewModel { Query = query });
        }

        var vm = new TestimonialListViewModel
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
        ViewData["Title"] = "Müvekkil Yorumu Detayı";

        var result = await _service.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Testimonial.NotFound)
                return NotFound();

            TempData["Error"] = "Müvekkil yorumu yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Yeni Müvekkil Yorumu";

        var vm = new TestimonialFormViewModel
        {
            IsActive = true,
            AuthorRole = "Müvekkil",
            DisplayOrder = 0,
            Translations = LanguageCodes.Supported
                .Select(lang => new TestimonialTranslationFormViewModel { LanguageCode = lang })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TestimonialFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Müvekkil Yorumu";

        var input = form.Adapt<TestimonialInputDto>();
        var result = await _service.CreateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "Müvekkil yorumu başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Müvekkil Yorumu Düzenle";

        var result = await _service.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Testimonial.NotFound)
                return NotFound();

            TempData["Error"] = "Müvekkil yorumu yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = result.Value.Adapt<TestimonialFormViewModel>();

        var existingLangs = vm.Translations.Select(t => t.LanguageCode).ToHashSet();
        foreach (var lang in LanguageCodes.Supported)
        {
            if (!existingLangs.Contains(lang))
            {
                vm.Translations.Add(new TestimonialTranslationFormViewModel { LanguageCode = lang });
            }
        }

        vm.Translations = vm.Translations
            .OrderBy(t => Array.IndexOf(LanguageCodes.Supported, t.LanguageCode))
            .ToList();

        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TestimonialFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Müvekkil Yorumu Düzenle";

        if (form.Id != id)
            return BadRequest();

        var input = form.Adapt<TestimonialInputDto>();
        input.Id = id;

        var result = await _service.UpdateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "Müvekkil yorumu başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Testimonial.NotFound)
                return NotFound();

            _logger.LogWarning("Müvekkil yorumu silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Müvekkil yorumu silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Müvekkil yorumu silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _service.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Testimonial.NotFound)
                return NotFound();

            _logger.LogWarning("Müvekkil yorumu geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Müvekkil yorumu geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Müvekkil yorumu geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var result = await _service.HardDeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Testimonial.NotFound)
                return NotFound();

            _logger.LogWarning("Müvekkil yorumu kalıcı silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Müvekkil yorumu kalıcı olarak silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Müvekkil yorumu kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Service;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Services;
using KucukMericHukuk.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/services")]
[Authorize(Roles = "Admin")]
public class ServicesController : Controller
{
    private readonly IServiceService _serviceService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceService serviceService,
        IUnitOfWork uow,
        ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _uow = uow;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(ServiceQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Hizmetler";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;
        if (string.IsNullOrWhiteSpace(query.LanguageCode)) query.LanguageCode = "tr-TR";

        var result = await _serviceService.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Hizmet listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Hizmet listesi yüklenirken bir sorun oluştu.";
            return View(new ServiceListViewModel { Query = query });
        }

        var vm = new ServiceListViewModel
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
        ViewData["Title"] = "Hizmet Detayı";

        var result = await _serviceService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Service.NotFound)
                return NotFound();

            TempData["Error"] = "Hizmet detayı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Hizmet";

        var vm = new ServiceFormViewModel
        {
            IsActive = true,
            DisplayOrder = 0,
            Translations = LanguageCodes.Supported
                .Select(lang => new ServiceTranslationFormViewModel { LanguageCode = lang })
                .ToList()
        };

        await LoadAvailableAttorneysAsync(vm, ct);
        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Hizmet";

        var input = form.Adapt<ServiceInputDto>();
        var result = await _serviceService.CreateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await LoadAvailableAttorneysAsync(form, ct);
            return View(form);
        }

        TempData["Success"] = "Hizmet başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Hizmet Düzenle";

        var result = await _serviceService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Service.NotFound)
                return NotFound();

            TempData["Error"] = "Hizmet yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = result.Value.Adapt<ServiceFormViewModel>();

        var existingLangs = vm.Translations.Select(t => t.LanguageCode).ToHashSet();
        foreach (var lang in LanguageCodes.Supported)
        {
            if (!existingLangs.Contains(lang))
            {
                vm.Translations.Add(new ServiceTranslationFormViewModel { LanguageCode = lang });
            }
        }

        vm.Translations = vm.Translations
            .OrderBy(t => Array.IndexOf(LanguageCodes.Supported, t.LanguageCode))
            .ToList();

        await LoadAvailableAttorneysAsync(vm, ct);
        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Hizmet Düzenle";

        if (form.Id != id)
            return BadRequest();

        var input = form.Adapt<ServiceInputDto>();
        input.Id = id;

        var result = await _serviceService.UpdateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await LoadAvailableAttorneysAsync(form, ct);
            return View(form);
        }

        TempData["Success"] = "Hizmet başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _serviceService.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Service.NotFound)
                return NotFound();

            _logger.LogWarning("Hizmet silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Hizmet silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Hizmet silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _serviceService.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Service.NotFound)
                return NotFound();

            _logger.LogWarning("Hizmet geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Hizmet geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Hizmet geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var result = await _serviceService.HardDeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Service.NotFound)
                return NotFound();

            _logger.LogWarning("Hizmet kalıcı olarak silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Hizmet kalıcı olarak silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Hizmet kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAvailableAttorneysAsync(
        ServiceFormViewModel vm, CancellationToken ct)
    {
        var attorneys = await _uow.Attorneys.GetActiveOrderedAsync(LanguageCodes.Turkish, ct);

        vm.AvailableAttorneys = attorneys.Select(a => new LookupDto
        {
            Id = a.Id,
            Name = a.Translations?.FirstOrDefault(t => t.LanguageCode == LanguageCodes.Turkish)?.FullName
                ?? a.Translations?.FirstOrDefault()?.FullName
                ?? "(adsız)"
        }).ToList();
    }
}

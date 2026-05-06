using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Attorneys;
using KucukMericHukuk.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/attorneys")]
[Authorize(Roles = "Admin,Editor")]
public class AttorneysController : Controller
{
    private readonly IAttorneyService _attorneyService;
    private readonly IUnitOfWork _uow;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AttorneysController> _logger;

    public AttorneysController(
        IAttorneyService attorneyService,
        IUnitOfWork uow,
        UserManager<ApplicationUser> userManager,
        ILogger<AttorneysController> logger)
    {
        _attorneyService = attorneyService;
        _uow = uow;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(AttorneyQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Avukatlarımız";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;
        if (string.IsNullOrWhiteSpace(query.LanguageCode)) query.LanguageCode = LanguageCodes.Turkish;

        var result = await _attorneyService.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Avukat listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Avukat listesi yüklenirken bir sorun oluştu.";
            var emptyVm = new AttorneyListViewModel { Query = query };
            emptyVm.AvailableServices = await LoadActiveServicesLookupAsync(ct);
            return View(emptyVm);
        }

        var vm = new AttorneyListViewModel
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

        vm.AvailableServices = await LoadActiveServicesLookupAsync(ct);

        return View(vm);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Avukat Detayı";

        var result = await _attorneyService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Attorney.NotFound)
                return NotFound();

            TempData["Error"] = "Avukat detayı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Avukat";

        var vm = new AttorneyFormViewModel
        {
            IsActive = true,
            DisplayOrder = 0,
            Translations = LanguageCodes.Supported
                .Select(lang => new AttorneyTranslationFormViewModel { LanguageCode = lang })
                .ToList()
        };

        await LoadAvailableUsersAsync(vm, ct);
        await LoadAvailableServicesAsync(vm, ct);
        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttorneyFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Avukat";

        var input = form.Adapt<AttorneyInputDto>();
        var result = await _attorneyService.CreateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await LoadAvailableUsersAsync(form, ct);
            await LoadAvailableServicesAsync(form, ct);
            return View(form);
        }

        TempData["Success"] = "Avukat başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Avukat Düzenle";

        var result = await _attorneyService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Attorney.NotFound)
                return NotFound();

            TempData["Error"] = "Avukat yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = result.Value.Adapt<AttorneyFormViewModel>();

        if (vm.ServiceIds.Count == 0 && result.Value.Services.Count > 0)
        {
            vm.ServiceIds = result.Value.Services.Select(s => s.Id).ToList();
        }

        var existingLangs = vm.Translations.Select(t => t.LanguageCode).ToHashSet();
        foreach (var lang in LanguageCodes.Supported)
        {
            if (!existingLangs.Contains(lang))
            {
                vm.Translations.Add(new AttorneyTranslationFormViewModel { LanguageCode = lang });
            }
        }

        vm.Translations = vm.Translations
            .OrderBy(t => Array.IndexOf(LanguageCodes.Supported, t.LanguageCode))
            .ToList();

        await LoadAvailableUsersAsync(vm, ct);
        await LoadAvailableServicesAsync(vm, ct);
        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AttorneyFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Avukat Düzenle";

        if (form.Id != id)
            return BadRequest();

        var input = form.Adapt<AttorneyInputDto>();
        input.Id = id;

        var result = await _attorneyService.UpdateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await LoadAvailableUsersAsync(form, ct);
            await LoadAvailableServicesAsync(form, ct);
            return View(form);
        }

        TempData["Success"] = "Avukat başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _attorneyService.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Attorney.NotFound)
                return NotFound();

            _logger.LogWarning("Avukat silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Avukat silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Avukat silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _attorneyService.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Attorney.NotFound)
                return NotFound();

            _logger.LogWarning("Avukat geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Avukat geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Avukat geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var result = await _attorneyService.HardDeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Attorney.NotFound)
                return NotFound();

            _logger.LogWarning("Avukat kalıcı olarak silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Avukat kalıcı olarak silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Avukat kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAvailableUsersAsync(
        AttorneyFormViewModel vm, CancellationToken ct)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync(ct);

        vm.AvailableUsers = users.Select(u => new LookupDto
        {
            Id = u.Id,
            Name = !string.IsNullOrWhiteSpace(u.FullName)
                ? $"{u.FullName} ({u.Email})"
                : u.Email ?? $"User #{u.Id}"
        }).ToList();
    }

    private async Task LoadAvailableServicesAsync(
        AttorneyFormViewModel vm, CancellationToken ct)
    {
        vm.AvailableServices = await LoadActiveServicesLookupAsync(ct);
    }

    private async Task<List<LookupDto>> LoadActiveServicesLookupAsync(CancellationToken ct)
    {
        var services = await _uow.Services.GetActiveOrderedAsync(LanguageCodes.Turkish, ct);

        return services.Select(s => new LookupDto
        {
            Id = s.Id,
            Name = s.Translations?.FirstOrDefault(t => t.LanguageCode == LanguageCodes.Turkish)?.Name
                ?? s.Translations?.FirstOrDefault()?.Name
                ?? "(adsız)"
        }).ToList();
    }
}

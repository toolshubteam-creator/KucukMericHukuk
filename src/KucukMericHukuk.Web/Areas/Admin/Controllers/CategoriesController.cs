using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Category;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Categories;
using KucukMericHukuk.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/categories")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryService categoryService,
        IUnitOfWork uow,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _uow = uow;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(CategoryQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Kategoriler";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;
        if (string.IsNullOrWhiteSpace(query.LanguageCode)) query.LanguageCode = "tr-TR";

        var result = await _categoryService.GetPagedAsync(query, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Kategori listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Kategori listesi yüklenirken bir sorun oluştu.";
            var emptyVm = new CategoryListViewModel { Query = query };
            emptyVm.AvailableParents = await BuildIndentedParentListAsync(excludeId: null, ct);
            return View(emptyVm);
        }

        var vm = new CategoryListViewModel
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
        vm.AvailableParents = await BuildIndentedParentListAsync(excludeId: null, ct);

        return View(vm);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Kategori Detayı";

        var result = await _categoryService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Category.NotFound)
                return NotFound();

            TempData["Error"] = "Kategori detayı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Kategori";

        var vm = new CategoryFormViewModel
        {
            IsActive = true,
            DisplayOrder = 0,
            Translations = LanguageCodes.Supported
                .Select(lang => new CategoryTranslationFormViewModel { LanguageCode = lang })
                .ToList()
        };

        await LoadAvailableParentCategoriesAsync(vm, excludeId: null, ct);
        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Kategori";

        var input = form.Adapt<CategoryInputDto>();
        var result = await _categoryService.CreateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await LoadAvailableParentCategoriesAsync(form, excludeId: null, ct);
            return View(form);
        }

        TempData["Success"] = "Kategori başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Kategori Düzenle";

        var result = await _categoryService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Category.NotFound)
                return NotFound();

            TempData["Error"] = "Kategori yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = result.Value.Adapt<CategoryFormViewModel>();

        var existingLangs = vm.Translations.Select(t => t.LanguageCode).ToHashSet();
        foreach (var lang in LanguageCodes.Supported)
        {
            if (!existingLangs.Contains(lang))
            {
                vm.Translations.Add(new CategoryTranslationFormViewModel { LanguageCode = lang });
            }
        }

        vm.Translations = vm.Translations
            .OrderBy(t => Array.IndexOf(LanguageCodes.Supported, t.LanguageCode))
            .ToList();

        await LoadAvailableParentCategoriesAsync(vm, excludeId: id, ct);
        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Kategori Düzenle";

        if (form.Id != id)
            return BadRequest();

        var input = form.Adapt<CategoryInputDto>();
        input.Id = id;

        var result = await _categoryService.UpdateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await LoadAvailableParentCategoriesAsync(form, excludeId: id, ct);
            return View(form);
        }

        TempData["Success"] = "Kategori başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _categoryService.DeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Category.NotFound)
                return NotFound();

            _logger.LogWarning("Kategori silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Kategori silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Kategori silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _categoryService.RestoreAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Category.NotFound)
                return NotFound();

            _logger.LogWarning("Kategori geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Kategori geri yüklenirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Kategori geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var result = await _categoryService.HardDeleteAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Category.NotFound)
                return NotFound();

            _logger.LogWarning("Kategori kalıcı olarak silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));

            TempData["Error"] = result.FirstError?.Message
                ?? "Kategori kalıcı olarak silinirken bir sorun oluştu.";

            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Kategori kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAvailableParentCategoriesAsync(
        CategoryFormViewModel vm, int? excludeId, CancellationToken ct)
    {
        vm.AvailableParents = await BuildIndentedParentListAsync(excludeId, ct);
    }

    private async Task<List<LookupDto>> BuildIndentedParentListAsync(
        int? excludeId, CancellationToken ct)
    {
        var allCategories = await _uow.Categories.GetAllActiveWithTranslationsAsync(LanguageCodes.Turkish, ct);

        var excluded = new HashSet<int>();
        if (excludeId.HasValue)
        {
            excluded.Add(excludeId.Value);
            var descendants = await _uow.Categories.GetDescendantIdsAsync(excludeId.Value, ct);
            foreach (var d in descendants)
                excluded.Add(d);
        }

        var lookup = allCategories
            .Where(c => !excluded.Contains(c.Id))
            .ToDictionary(c => c.Id, c => c);

        var result = new List<LookupDto>();

        void TraverseDepthFirst(Category cat, int depth)
        {
            var indent = depth > 0
                ? string.Concat(Enumerable.Repeat("    ", depth)) + "— "
                : string.Empty;

            var name = cat.Translations?.FirstOrDefault()?.Name ?? "(adsız)";

            result.Add(new LookupDto
            {
                Id = cat.Id,
                Name = indent + name
            });

            var children = lookup.Values
                .Where(c => c.ParentCategoryId == cat.Id)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Id);

            foreach (var child in children)
            {
                TraverseDepthFirst(child, depth + 1);
            }
        }

        var roots = lookup.Values
            .Where(c => !c.ParentCategoryId.HasValue ||
                        !lookup.ContainsKey(c.ParentCategoryId.Value))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id);

        foreach (var root in roots)
        {
            TraverseDepthFirst(root, 0);
        }

        return result;
    }
}

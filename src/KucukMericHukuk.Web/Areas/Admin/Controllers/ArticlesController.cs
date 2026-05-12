using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Articles;
using KucukMericHukuk.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/articles")]
[Authorize(Roles = "Admin,Editor")]
public class ArticlesController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(
        IArticleService articleService,
        IUnitOfWork uow,
        ILogger<ArticlesController> logger)
    {
        _articleService = articleService;
        _uow = uow;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(ArticleListQuery query, CancellationToken ct)
    {
        ViewData["Title"] = "Makaleler";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;

        var serviceQuery = new ArticleQueryDto
        {
            Keyword = query.Keyword,
            LanguageCode = LanguageCodes.Default,
            Status = query.Status,
            CategoryId = query.CategoryId,
            Page = query.Page,
            PageSize = query.PageSize,
            IncludeDeleted = query.IncludeDeleted,
            AuthorIdFilter = User.IsInRole("Admin") ? null : User.GetUserIdOrNull(),
        };

        var categoryLookups = await GetCategoryLookupsAsync(ct);

        var result = await _articleService.GetPagedAsync(serviceQuery, ct);
        if (result.IsFailure)
        {
            _logger.LogError("Makale listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => $"[{e.Code}] {e.Message}")));
            TempData["Error"] = "Makale listesi yüklenirken bir sorun oluştu.";
            return View(new ArticleListViewModel { Query = query, Categories = categoryLookups });
        }

        var vm = new ArticleListViewModel
        {
            Query = query,
            Items = result.Value.Items,
            TotalCount = result.Value.TotalCount,
            PageNumber = result.Value.PageNumber,
            PageSize = result.Value.PageSize,
            TotalPages = result.Value.TotalPages,
            HasPrevious = result.Value.HasPrevious,
            HasNext = result.Value.HasNext,
            Categories = categoryLookups,
        };
        return View(vm);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Makale Detayı";

        var result = await _articleService.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Article.NotFound)
                return NotFound();

            TempData["Error"] = "Makale yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Makale";

        var vm = new ArticleFormViewModel
        {
            Status = Core.Enums.ArticleStatus.Draft,
            Translations = LanguageCodes.Supported
                .Select(lang => new ArticleTranslationFormViewModel { LanguageCode = lang })
                .ToList(),
        };
        await PopulateLookupsAsync(vm, ct);
        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Makale";

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(form, ct);
            return View(form);
        }

        var input = form.Adapt<ArticleInputDto>();
        input.AuthorId = User.GetUserIdOrNull();

        var result = await _articleService.CreateAsync(input, ct);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await PopulateLookupsAsync(form, ct);
            return View(form);
        }

        TempData["Success"] = "Makale oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Makale Düzenle";

        var guard = await EnsureCanEditAsync(id, ct);
        if (guard is not null) return guard;

        var result = await _articleService.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Article.NotFound)
                return NotFound();
            TempData["Error"] = "Makale yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Value;
        var vm = new ArticleFormViewModel
        {
            Id = dto.Id,
            AuthorId = dto.AuthorId,
            AuthorName = dto.AuthorName,
            CategoryId = dto.CategoryId,
            FeaturedImageUrl = dto.FeaturedImageUrl,
            PublishedAt = dto.PublishedAt,
            Status = dto.Status,
            IsFeatured = dto.IsFeatured,
            TagIds = dto.Tags.Select(t => t.Id).ToList(),
            Translations = LanguageCodes.Supported.Select(lang =>
            {
                var t = dto.Translations.FirstOrDefault(tr => tr.LanguageCode == lang);
                return new ArticleTranslationFormViewModel
                {
                    Id = t?.Id,
                    LanguageCode = lang,
                    Title = t?.Title ?? string.Empty,
                    Slug = t?.Slug,
                    Excerpt = t?.Excerpt,
                    Content = t?.Content ?? string.Empty,
                    MetaTitle = t?.MetaTitle,
                    MetaDescription = t?.MetaDescription,
                };
            }).ToList(),
        };
        await PopulateLookupsAsync(vm, ct);
        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ArticleFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Makale Düzenle";

        if (form.Id != id) return BadRequest();

        var guard = await EnsureCanEditAsync(id, ct);
        if (guard is not null) return guard;

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(form, ct);
            return View(form);
        }

        var input = form.Adapt<ArticleInputDto>();
        input.Id = id;
        input.AuthorId = User.IsInRole("Admin") ? form.AuthorId : User.GetUserIdOrNull();

        var result = await _articleService.UpdateAsync(input, ct);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            await PopulateLookupsAsync(form, ct);
            return View(form);
        }

        TempData["Success"] = "Makale güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var guard = await EnsureCanEditAsync(id, ct);
        if (guard is not null) return guard;

        var result = await _articleService.DeleteAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Article.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message ?? "Makale silinemedi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Makale silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var guard = await EnsureCanEditAsync(id, ct);
        if (guard is not null) return guard;

        var result = await _articleService.RestoreAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Article.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message ?? "Makale geri yüklenemedi.";
            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Makale geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var guard = await EnsureCanEditAsync(id, ct);
        if (guard is not null) return guard;

        var result = await _articleService.HardDeleteAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Article.NotFound)
                return NotFound();

            TempData["Error"] = result.FirstError?.Message ?? "Makale kalıcı silinemedi.";
            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }

        TempData["Success"] = "Makale kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult?> EnsureCanEditAsync(int articleId, CancellationToken ct)
    {
        if (User.IsInRole("Admin")) return null;

        var article = await _uow.Articles.GetByIdIncludingDeletedAsync(articleId, ct);
        if (article is null) return NotFound();

        var currentUserId = User.GetUserIdOrNull();
        if (currentUserId is null || article.AuthorId != currentUserId)
            return Forbid();

        return null;
    }

    private async Task<IReadOnlyList<LookupDto>> GetCategoryLookupsAsync(CancellationToken ct)
    {
        var cats = await _uow.Categories.GetActiveOrderedAsync(LanguageCodes.Default, ct);
        return cats
            .Select(c => new LookupDto
            {
                Id = c.Id,
                Name = c.Translations.FirstOrDefault()?.Name ?? $"#{c.Id}",
            })
            .ToList();
    }

    private async Task PopulateLookupsAsync(ArticleFormViewModel vm, CancellationToken ct)
    {
        vm.Categories = await GetCategoryLookupsAsync(ct);

        var tags = await _uow.Tags.GetAllOrderedAsync(LanguageCodes.Default, ct);
        vm.AvailableTags = tags
            .Select(t => new LookupDto
            {
                Id = t.Id,
                Name = t.Translations.FirstOrDefault()?.Name ?? $"#{t.Id}",
            })
            .ToList();
    }
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Page;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Pages;
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
}

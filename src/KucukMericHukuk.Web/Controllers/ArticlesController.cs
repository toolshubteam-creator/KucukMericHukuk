using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Article;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class ArticlesController : Controller
{
    private const int PageSize = 9;
    private const int RelatedCount = 3;

    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;

    public ArticlesController(IArticleService articleService, ICategoryService categoryService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
    }

    [HttpGet]
    [Route("{culture:culture}/Articles", Order = 0)]
    [Route("{culture:culture}/Articles/Index", Order = 1)]
    public async Task<IActionResult> Index(string? category, int page = 1, CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;
        if (page < 1) page = 1;

        var categories = await _categoryService.GetActiveOrderedAsync(lang, ct);
        var selectedCategory = string.IsNullOrEmpty(category)
            ? null
            : categories.FirstOrDefault(c => string.Equals(c.Slug, category, StringComparison.OrdinalIgnoreCase));

        var paged = selectedCategory is null
            ? await _articleService.GetPublishedPagedAsync(lang, page, PageSize, ct)
            : await _articleService.GetByCategoryAsync(selectedCategory.Id, lang, page, PageSize, ct);

        var vm = new ArticleListViewModel
        {
            Articles = paged,
            Categories = categories,
            SelectedCategorySlug = selectedCategory?.Slug
        };
        return View(vm);
    }

    [HttpGet]
    [Route("{culture:culture}/Articles/{slug:regex(^(?!Index$).+)}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;

        var result = await _articleService.GetBySlugAsync(lang, slug, ct);
        if (result.IsFailure)
        {
            return NotFound();
        }

        var article = result.Value;
        var related = await _articleService.GetRelatedAsync(
            article.Id, article.CategoryId, lang, RelatedCount, ct);

        var vm = new ArticleDetailViewModel
        {
            Article = article,
            RelatedArticles = related
        };
        return View(vm);
    }
}

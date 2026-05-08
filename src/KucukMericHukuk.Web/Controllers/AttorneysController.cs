using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Attorney;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class AttorneysController : Controller
{
    private const int AuthorArticlesCount = 3;

    private readonly IAttorneyService _attorneyService;
    private readonly IArticleService _articleService;

    public AttorneysController(
        IAttorneyService attorneyService,
        IArticleService articleService)
    {
        _attorneyService = attorneyService;
        _articleService = articleService;
    }

    [HttpGet]
    [Route("{culture:culture}/Attorneys", Order = 0)]
    [Route("{culture:culture}/Attorneys/Index", Order = 1)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;
        var attorneys = await _attorneyService.GetActiveOrderedAsync(lang, take: null, ct);

        var vm = new AttorneyListViewModel { Attorneys = attorneys };
        return View(vm);
    }

    [HttpGet]
    [Route("{culture:culture}/Attorneys/{slug:regex(^(?!Index$).+)}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;

        var result = await _attorneyService.GetBySlugAsync(slug, lang, ct);
        if (result.IsFailure)
        {
            return NotFound();
        }

        var attorney = result.Value;

        IReadOnlyList<ArticleListDto> authorArticles = [];
        if (attorney.UserId.HasValue)
        {
            authorArticles = await _articleService.GetByAuthorAsync(
                attorney.UserId.Value, lang, AuthorArticlesCount, ct);
        }

        var vm = new AttorneyDetailViewModel
        {
            Attorney = attorney,
            AuthorArticles = authorArticles
        };
        return View(vm);
    }
}

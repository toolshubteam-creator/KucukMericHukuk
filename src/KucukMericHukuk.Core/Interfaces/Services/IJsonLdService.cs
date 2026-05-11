using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Faq;

namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// Schema.org JSON-LD üreticisi. Her tip için ayrı method, JSON string döner.
/// </summary>
public interface IJsonLdService
{
    string BuildLegalService();
    string BuildWebSite();
    string BuildArticle(ArticleDetailDto article);
    string BuildPerson(AttorneyDetailDto attorney);
    string BuildFaqPage(IEnumerable<FaqListDto> faqs);

    /// <summary>
    /// BreadcrumbList schema. Her item position 1'den başlar.
    /// items: (label, absoluteUrl?) — son item URL null olabilir (current page).
    /// </summary>
    string BuildBreadcrumbList(IEnumerable<(string Label, string? Url)> items);
}

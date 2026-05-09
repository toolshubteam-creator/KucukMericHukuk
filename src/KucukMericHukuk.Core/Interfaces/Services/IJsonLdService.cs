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
}

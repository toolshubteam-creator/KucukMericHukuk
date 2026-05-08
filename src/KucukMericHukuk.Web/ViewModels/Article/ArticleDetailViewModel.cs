using KucukMericHukuk.Core.DTOs.Article;

namespace KucukMericHukuk.Web.ViewModels.Article;

public class ArticleDetailViewModel
{
    public ArticleDetailDto Article { get; init; } = new();
    public IReadOnlyList<ArticleListDto> RelatedArticles { get; init; } = [];
}

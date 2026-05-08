using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Attorney;

namespace KucukMericHukuk.Web.ViewModels.Attorney;

public class AttorneyDetailViewModel
{
    public AttorneyDetailDto Attorney { get; init; } = new();
    public IReadOnlyList<ArticleListDto> AuthorArticles { get; init; } = [];
}

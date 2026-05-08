using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Category;
using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Web.ViewModels.Article;

public class ArticleListViewModel
{
    public PagedResult<ArticleListDto> Articles { get; init; } = new();
    public IReadOnlyList<CategoryListDto> Categories { get; init; } = [];
    public string? SelectedCategorySlug { get; init; }
}

using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Articles;

public class ArticleListViewModel
{
    public ArticleListQuery Query { get; set; } = new();
    public IReadOnlyList<ArticleAdminDto> Items { get; set; } = Array.Empty<ArticleAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public IReadOnlyList<LookupDto> Categories { get; set; } = Array.Empty<LookupDto>();
}

public class ArticleListQuery
{
    public string? Keyword { get; set; }
    public ArticleStatus? Status { get; set; }
    public int? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; }
}

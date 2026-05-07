using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Article;

public class ArticleQueryDto
{
    public string? Keyword { get; set; }
    public string LanguageCode { get; set; } = LanguageCodes.Default;
    public ArticleStatus? Status { get; set; }
    public int? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; }
}

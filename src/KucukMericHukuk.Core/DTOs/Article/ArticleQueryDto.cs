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

    /// <summary>
    /// Editor için ownership filter — sadece bu AuthorId'ye ait makaleler döner.
    /// Admin için null (tümü). Controller tarafı current user'a göre doldurur (Faz 6.10).
    /// </summary>
    public int? AuthorIdFilter { get; set; }
}

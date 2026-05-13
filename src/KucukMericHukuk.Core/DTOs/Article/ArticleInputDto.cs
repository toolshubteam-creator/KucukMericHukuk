using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Article;

public class ArticleInputDto
{
    public int? Id { get; set; }
    public int? AuthorId { get; set; }
    public int? EditorId { get; set; }
    public int? CategoryId { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public bool IsFeatured { get; set; }
    public List<ArticleTranslationInputDto> Translations { get; set; } = new();
    public List<int> TagIds { get; set; } = new();
}

public class ArticleTranslationInputDto
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

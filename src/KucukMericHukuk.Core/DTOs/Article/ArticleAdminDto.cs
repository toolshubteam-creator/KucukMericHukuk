using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Article;

public class ArticleAdminDto
{
    public int Id { get; set; }
    public int? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public ArticleStatus Status { get; set; }
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public List<ArticleTranslationDto> Translations { get; set; } = new();
    public List<LookupDto> Tags { get; set; } = new();
}

public class ArticleTranslationDto : TranslationDto
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ReadingTimeMinutes { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

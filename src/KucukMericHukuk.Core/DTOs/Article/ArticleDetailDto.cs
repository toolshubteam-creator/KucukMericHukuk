using KucukMericHukuk.Core.DTOs.Tag;

namespace KucukMericHukuk.Core.DTOs.Article;

public class ArticleDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public int ViewCount { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string? AuthorName { get; set; }
    public int? AuthorId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public int? CategoryId { get; set; }
    public List<TagListDto> Tags { get; set; } = new();
}

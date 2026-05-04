namespace KucukMericHukuk.Core.DTOs.Article;

public class ArticleListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public int ViewCount { get; set; }
    public string? AuthorName { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
}

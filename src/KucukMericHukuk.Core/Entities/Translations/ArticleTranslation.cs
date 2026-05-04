namespace KucukMericHukuk.Core.Entities.Translations;

public class ArticleTranslation : BaseTranslation
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ReadingTimeMinutes { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

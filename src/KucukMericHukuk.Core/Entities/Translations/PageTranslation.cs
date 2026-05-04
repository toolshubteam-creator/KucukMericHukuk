namespace KucukMericHukuk.Core.Entities.Translations;

public class PageTranslation : BaseTranslation
{
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

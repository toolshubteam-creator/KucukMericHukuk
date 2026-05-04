namespace KucukMericHukuk.Core.DTOs.Page;

public class PageInputDto
{
    public int? Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public List<PageTranslationInputDto> Translations { get; set; } = new();
}

public class PageTranslationInputDto
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

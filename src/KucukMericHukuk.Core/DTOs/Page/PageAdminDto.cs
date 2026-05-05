using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.DTOs.Page;

public class PageAdminDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<PageTranslationDto> Translations { get; set; } = new();
}

public class PageTranslationDto : TranslationDto
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

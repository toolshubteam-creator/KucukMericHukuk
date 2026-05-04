using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.DTOs.Category;

public class CategoryAdminDto
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<CategoryTranslationDto> Translations { get; set; } = new();
    public int ArticleCount { get; set; }
}

public class CategoryTranslationDto : TranslationDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

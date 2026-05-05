using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.DTOs.Tag;

public class TagAdminDto
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public int ArticleCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TagTranslationDto> Translations { get; set; } = new();
}

public class TagTranslationDto : TranslationDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

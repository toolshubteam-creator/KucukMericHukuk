namespace KucukMericHukuk.Core.DTOs.Tag;

public class TagInputDto
{
    public int? Id { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TagTranslationInputDto> Translations { get; set; } = new();
}

public class TagTranslationInputDto
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

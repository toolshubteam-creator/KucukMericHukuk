namespace KucukMericHukuk.Core.DTOs.Faq;

public class FaqInputDto
{
    public int? Id { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<FaqTranslationInputDto> Translations { get; set; } = new();
}

public class FaqTranslationInputDto
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

namespace KucukMericHukuk.Core.DTOs.Faq;

public class FaqListDto
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
}

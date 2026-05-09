namespace KucukMericHukuk.Core.Entities.Translations;

public class FaqTranslation : BaseTranslation
{
    public int FaqId { get; set; }
    public Faq Faq { get; set; } = null!;

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

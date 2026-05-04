namespace KucukMericHukuk.Core.Entities.Translations;

public class TagTranslation : BaseTranslation
{
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

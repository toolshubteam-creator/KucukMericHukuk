namespace KucukMericHukuk.Core.Entities.Translations;

public class ServiceTranslation : BaseTranslation
{
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

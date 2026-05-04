namespace KucukMericHukuk.Core.Entities.Translations;

public class AttorneyTranslation : BaseTranslation
{
    public int AttorneyId { get; set; }
    public Attorney Attorney { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? ShortBio { get; set; }
    public string? FullBio { get; set; }
    public string? Education { get; set; }
    public string? Publications { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

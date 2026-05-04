namespace KucukMericHukuk.Core.DTOs.Service;

public class ServiceInputDto
{
    public int? Id { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? FeaturedImage { get; set; }
    public List<ServiceTranslationInputDto> Translations { get; set; } = new();
    public List<int> AttorneyIds { get; set; } = new();
}

public class ServiceTranslationInputDto
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

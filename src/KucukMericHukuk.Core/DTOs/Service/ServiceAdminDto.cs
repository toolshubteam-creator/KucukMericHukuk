using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.DTOs.Service;

public class ServiceAdminDto
{
    public int Id { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string? FeaturedImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ServiceTranslationDto> Translations { get; set; } = new();
    public List<LookupDto> Attorneys { get; set; } = new();
}

public class ServiceTranslationDto : TranslationDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

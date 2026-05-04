namespace KucukMericHukuk.Core.DTOs.Attorney;

public class AttorneyInputDto
{
    public int? Id { get; set; }
    public int? UserId { get; set; }
    public string? BarRegistrationNumber { get; set; }
    public string? BarName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<AttorneyTranslationInputDto> Translations { get; set; } = new();
    public List<int> ServiceIds { get; set; } = new();
}

public class AttorneyTranslationInputDto
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
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

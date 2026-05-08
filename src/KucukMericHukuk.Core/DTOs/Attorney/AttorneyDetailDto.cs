using KucukMericHukuk.Core.DTOs.Service;

namespace KucukMericHukuk.Core.DTOs.Attorney;

public class AttorneyDetailDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? ShortBio { get; set; }
    public string? FullBio { get; set; }
    public string? Education { get; set; }
    public string? Publications { get; set; }
    public string? BarRegistrationNumber { get; set; }
    public string? BarName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public List<ServiceListDto> Services { get; set; } = new();
}

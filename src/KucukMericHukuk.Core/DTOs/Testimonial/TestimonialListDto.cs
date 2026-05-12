namespace KucukMericHukuk.Core.DTOs.Testimonial;

/// <summary>Public liste/grid DTO — translation flatten (Mapster). Tek dil odaklı.</summary>
public class TestimonialListDto
{
    public int Id { get; set; }
    public string? AuthorInitials { get; set; }
    public string? AuthorRole { get; set; }
    public int? Rating { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public string Content { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
}

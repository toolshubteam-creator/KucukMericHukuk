using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.DTOs.Testimonial;

public class TestimonialAdminDto
{
    public int Id { get; set; }
    public string? AuthorInitials { get; set; }
    public string? AuthorRole { get; set; }
    public int? Rating { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TestimonialTranslationDto> Translations { get; set; } = new();
}

public class TestimonialTranslationDto : TranslationDto
{
    public string Content { get; set; } = string.Empty;
}

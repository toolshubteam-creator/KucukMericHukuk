namespace KucukMericHukuk.Core.DTOs.Testimonial;

public class TestimonialInputDto
{
    public int? Id { get; set; }
    public string? AuthorInitials { get; set; }
    public string? AuthorRole { get; set; }
    public int? Rating { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public List<TestimonialTranslationInputDto> Translations { get; set; } = new();
}

public class TestimonialTranslationInputDto
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

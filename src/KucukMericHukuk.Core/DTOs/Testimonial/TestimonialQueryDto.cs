using KucukMericHukuk.Core.Constants;

namespace KucukMericHukuk.Core.DTOs.Testimonial;

public class TestimonialQueryDto
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; } = false;
    public string LanguageCode { get; set; } = LanguageCodes.Turkish;
}

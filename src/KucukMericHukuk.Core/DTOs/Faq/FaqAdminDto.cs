using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.DTOs.Faq;

public class FaqAdminDto
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<FaqTranslationDto> Translations { get; set; } = new();
}

public class FaqTranslationDto : TranslationDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

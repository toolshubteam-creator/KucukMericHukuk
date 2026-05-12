namespace KucukMericHukuk.Core.DTOs.Faq;

/// <summary>
/// Faq sıralaması için tek bir kayıt — drag-drop UI sonrası batch update payload'unun elemanı.
/// </summary>
public class FaqReorderItemDto
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
}

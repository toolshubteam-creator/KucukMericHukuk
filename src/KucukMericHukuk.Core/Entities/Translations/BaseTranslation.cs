using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities.Translations;

public abstract class BaseTranslation : ITranslation
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = LanguageCodes.Default;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

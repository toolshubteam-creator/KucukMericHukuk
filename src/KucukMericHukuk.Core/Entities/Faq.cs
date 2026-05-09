using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities;

public class Faq : BaseEntity, ITranslatable<FaqTranslation>
{
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    public ICollection<FaqTranslation> Translations { get; set; } = new List<FaqTranslation>();
}

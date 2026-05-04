using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities;

public class Tag : BaseEntity, ITranslatable<TagTranslation>
{
    public ICollection<TagTranslation> Translations { get; set; } = new List<TagTranslation>();
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}

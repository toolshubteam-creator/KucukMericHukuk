using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities;

public class Page : BaseEntity, ITranslatable<PageTranslation>
{
    public string PageKey { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    public ICollection<PageTranslation> Translations { get; set; } = new List<PageTranslation>();
}

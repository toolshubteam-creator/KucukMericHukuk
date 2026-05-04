using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities;

public class Service : BaseEntity, ITranslatable<ServiceTranslation>
{
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? FeaturedImage { get; set; }

    public ICollection<ServiceTranslation> Translations { get; set; } = new List<ServiceTranslation>();
    public ICollection<Attorney> Attorneys { get; set; } = new List<Attorney>();
}

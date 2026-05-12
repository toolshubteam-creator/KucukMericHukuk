using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities;

public class Testimonial : BaseEntity, ITranslatable<TestimonialTranslation>
{
    /// <summary>TBB reklam yasağı: inisyal/rumuz (örn. "M.A."), tam ad değil.</summary>
    public string? AuthorInitials { get; set; }

    /// <summary>Nötr rol (default "Müvekkil"). Davayla ilişki ifşa edilmez.</summary>
    public string? AuthorRole { get; set; }

    /// <summary>1-5 yıldız, nullable.</summary>
    public int? Rating { get; set; }

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    public ICollection<TestimonialTranslation> Translations { get; set; } = new List<TestimonialTranslation>();
}

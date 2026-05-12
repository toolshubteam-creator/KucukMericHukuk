namespace KucukMericHukuk.Core.Entities.Translations;

public class TestimonialTranslation : BaseTranslation
{
    public int TestimonialId { get; set; }
    public Testimonial Testimonial { get; set; } = null!;

    /// <summary>Müvekkil yorumu — plain text, dava/firma detayı YASAK (TBB).</summary>
    public string Content { get; set; } = string.Empty;
}

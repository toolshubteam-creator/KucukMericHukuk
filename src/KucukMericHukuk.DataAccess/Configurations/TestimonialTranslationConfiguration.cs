using KucukMericHukuk.Core.Entities.Translations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class TestimonialTranslationConfiguration : IEntityTypeConfiguration<TestimonialTranslation>
{
    public void Configure(EntityTypeBuilder<TestimonialTranslation> builder)
    {
        builder.ToTable("TestimonialTranslations");
        builder.HasQueryFilter(t => !t.Testimonial.IsDeleted);

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Content).IsRequired().HasMaxLength(2000);

        builder.HasIndex(t => new { t.TestimonialId, t.LanguageCode }).IsUnique();
    }
}

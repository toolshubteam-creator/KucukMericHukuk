using KucukMericHukuk.Core.Entities.Translations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class FaqTranslationConfiguration : IEntityTypeConfiguration<FaqTranslation>
{
    public void Configure(EntityTypeBuilder<FaqTranslation> builder)
    {
        builder.ToTable("FaqTranslations");
        builder.HasQueryFilter(t => !t.Faq.IsDeleted);

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Question).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Answer).IsRequired();

        builder.HasIndex(t => new { t.FaqId, t.LanguageCode }).IsUnique();
    }
}

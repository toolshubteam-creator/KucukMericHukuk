using KucukMericHukuk.Core.Entities.Translations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class TagTranslationConfiguration : IEntityTypeConfiguration<TagTranslation>
{
    public void Configure(EntityTypeBuilder<TagTranslation> builder)
    {
        builder.ToTable("TagTranslations");
        builder.HasQueryFilter(t => !t.Tag.IsDeleted);

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(250);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(250);

        builder.HasIndex(t => new { t.LanguageCode, t.Slug }).IsUnique();
        builder.HasIndex(t => new { t.LanguageCode, t.TagId }).IsUnique();
    }
}

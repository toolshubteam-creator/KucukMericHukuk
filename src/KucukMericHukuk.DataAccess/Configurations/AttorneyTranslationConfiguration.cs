using KucukMericHukuk.Core.Entities.Translations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class AttorneyTranslationConfiguration : IEntityTypeConfiguration<AttorneyTranslation>
{
    public void Configure(EntityTypeBuilder<AttorneyTranslation> builder)
    {
        builder.ToTable("AttorneyTranslations");
        builder.HasQueryFilter(t => !t.Attorney.IsDeleted);

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.FullName).IsRequired().HasMaxLength(250);
        builder.Property(t => t.Title).HasMaxLength(50);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(250);
        builder.Property(t => t.ShortBio).HasMaxLength(500);
        builder.Property(t => t.MetaTitle).HasMaxLength(250);
        builder.Property(t => t.MetaDescription).HasMaxLength(500);

        builder.HasIndex(t => new { t.LanguageCode, t.Slug }).IsUnique();
        builder.HasIndex(t => new { t.LanguageCode, t.AttorneyId }).IsUnique();
    }
}

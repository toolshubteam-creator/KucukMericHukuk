using KucukMericHukuk.Core.Entities.Translations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class PageTranslationConfiguration : IEntityTypeConfiguration<PageTranslation>
{
    public void Configure(EntityTypeBuilder<PageTranslation> builder)
    {
        builder.ToTable("PageTranslations");
        builder.HasQueryFilter(t => !t.Page.IsDeleted);

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(250);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(250);
        builder.Property(t => t.MetaTitle).HasMaxLength(250);
        builder.Property(t => t.MetaDescription).HasMaxLength(500);

        builder.HasIndex(t => new { t.LanguageCode, t.Slug }).IsUnique();
        builder.HasIndex(t => new { t.LanguageCode, t.PageId }).IsUnique();
    }
}

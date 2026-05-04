using KucukMericHukuk.Core.Entities.Translations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class ArticleTranslationConfiguration : IEntityTypeConfiguration<ArticleTranslation>
{
    public void Configure(EntityTypeBuilder<ArticleTranslation> builder)
    {
        builder.ToTable("ArticleTranslations");
        builder.HasQueryFilter(t => !t.Article.IsDeleted);

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(250);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(250);
        builder.Property(t => t.Excerpt).HasMaxLength(500);
        builder.Property(t => t.MetaTitle).HasMaxLength(250);
        builder.Property(t => t.MetaDescription).HasMaxLength(500);

        builder.HasIndex(t => new { t.LanguageCode, t.Slug }).IsUnique();
        builder.HasIndex(t => new { t.LanguageCode, t.ArticleId }).IsUnique();
    }
}

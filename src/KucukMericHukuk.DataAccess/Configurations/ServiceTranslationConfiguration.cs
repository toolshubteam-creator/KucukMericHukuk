using KucukMericHukuk.Core.Entities.Translations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class ServiceTranslationConfiguration : IEntityTypeConfiguration<ServiceTranslation>
{
    public void Configure(EntityTypeBuilder<ServiceTranslation> builder)
    {
        builder.ToTable("ServiceTranslations");
        builder.HasQueryFilter(t => !t.Service.IsDeleted);

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(250);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(250);
        builder.Property(t => t.ShortDescription).HasMaxLength(500);
        builder.Property(t => t.MetaTitle).HasMaxLength(250);
        builder.Property(t => t.MetaDescription).HasMaxLength(500);

        builder.HasIndex(t => new { t.LanguageCode, t.Slug }).IsUnique();
        builder.HasIndex(t => new { t.LanguageCode, t.ServiceId }).IsUnique();
    }
}

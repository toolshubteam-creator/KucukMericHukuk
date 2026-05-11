using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("SiteSettings");
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.Property(s => s.Key).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Value).HasMaxLength(4000);
        builder.Property(s => s.Group).IsRequired().HasMaxLength(50);
        builder.Property(s => s.DataType).IsRequired().HasMaxLength(20).HasDefaultValue("string");
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(s => s.Key).IsUnique();
        builder.HasIndex(s => s.Group);
        builder.HasIndex(s => s.IsDeleted);
    }
}

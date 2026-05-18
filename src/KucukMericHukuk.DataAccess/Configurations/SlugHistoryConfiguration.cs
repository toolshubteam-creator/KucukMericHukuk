using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class SlugHistoryConfiguration : IEntityTypeConfiguration<SlugHistory>
{
    public void Configure(EntityTypeBuilder<SlugHistory> builder)
    {
        builder.ToTable("SlugHistories");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.EntityType).HasConversion<int>();
        builder.Property(s => s.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(s => s.OldSlug).IsRequired().HasMaxLength(200);

        // Composite NON-UNIQUE: slug X→Y→X senaryosunda aynı (EntityType, Lang, OldSlug)
        // için iki satır oluşabilir; repo FindCurrentAsync `CreatedAt DESC.First` ile
        // en yeni eşleşmeyi seçer (entity'nin GÜNCEL slug'ına yönlendirir).
        builder.HasIndex(s => new { s.EntityType, s.LanguageCode, s.OldSlug });

        // Entity'ye göre history listeleme (admin debug) için ek index.
        builder.HasIndex(s => new { s.EntityType, s.EntityId });
    }
}

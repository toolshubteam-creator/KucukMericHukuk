using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.Property(s => s.Icon).HasMaxLength(50);
        builder.Property(s => s.FeaturedImage).HasMaxLength(500);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(s => s.IsDeleted);
        builder.HasIndex(s => s.DisplayOrder);

        builder.HasMany(s => s.Translations)
            .WithOne(t => t.Service)
            .HasForeignKey(t => t.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Attorneys)
            .WithMany(a => a.Services)
            .UsingEntity("AttorneyServices");
    }
}

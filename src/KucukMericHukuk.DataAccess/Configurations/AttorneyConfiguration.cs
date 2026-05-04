using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class AttorneyConfiguration : IEntityTypeConfiguration<Attorney>
{
    public void Configure(EntityTypeBuilder<Attorney> builder)
    {
        builder.ToTable("Attorneys");
        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Property(a => a.BarRegistrationNumber).HasMaxLength(50);
        builder.Property(a => a.BarName).HasMaxLength(150);
        builder.Property(a => a.Email).HasMaxLength(150);
        builder.Property(a => a.PhoneNumber).HasMaxLength(50);
        builder.Property(a => a.LinkedInUrl).HasMaxLength(500);
        builder.Property(a => a.ProfileImageUrl).HasMaxLength(500);

        builder.HasIndex(a => a.IsDeleted);
        builder.HasIndex(a => a.DisplayOrder);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Translations)
            .WithOne(t => t.Attorney)
            .HasForeignKey(t => t.AttorneyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

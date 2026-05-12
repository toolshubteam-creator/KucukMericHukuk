using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> builder)
    {
        builder.ToTable("Testimonials");
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.Property(t => t.AuthorInitials).HasMaxLength(10);
        builder.Property(t => t.AuthorRole).HasMaxLength(100);
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.IsFeatured).HasDefaultValue(false);
        builder.Property(t => t.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(t => t.IsDeleted);
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.DisplayOrder);
        builder.HasIndex(t => t.IsFeatured);

        builder.HasMany(t => t.Translations)
            .WithOne(tr => tr.Testimonial)
            .HasForeignKey(tr => tr.TestimonialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

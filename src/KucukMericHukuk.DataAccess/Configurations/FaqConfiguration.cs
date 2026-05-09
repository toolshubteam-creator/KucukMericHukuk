using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("Faqs");
        builder.HasQueryFilter(f => !f.IsDeleted);

        builder.Property(f => f.IsActive).HasDefaultValue(true);
        builder.Property(f => f.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(f => f.IsDeleted);
        builder.HasIndex(f => f.DisplayOrder);
        builder.HasIndex(f => f.IsActive);

        builder.HasMany(f => f.Translations)
            .WithOne(t => t.Faq)
            .HasForeignKey(t => t.FaqId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

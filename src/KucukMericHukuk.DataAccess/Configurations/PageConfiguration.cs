using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.PageKey).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.PageKey).IsUnique();
        builder.HasIndex(p => p.IsDeleted);

        builder.HasMany(p => p.Translations)
            .WithOne(t => t.Page)
            .HasForeignKey(t => t.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

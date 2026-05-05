using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(t => t.IsDeleted);

        builder.HasMany(t => t.Articles)
            .WithMany(a => a.Tags)
            .UsingEntity("ArticleTags");

        builder.HasMany(t => t.Translations)
            .WithOne(tr => tr.Tag)
            .HasForeignKey(tr => tr.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

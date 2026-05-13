using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");
        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Property(a => a.FeaturedImageUrl).HasMaxLength(500);

        builder.HasIndex(a => a.IsDeleted);
        builder.HasIndex(a => a.PublishedAt);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.IsFeatured);

        builder.HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        // ApplicationUser soft-delete kullandığı için hard-delete senaryosu yok.
        // SQL Server multi-cascade-path engeli için Restrict — Author zaten SetNull;
        // ikisi birden SetNull olursa "cycles or multiple cascade paths" hatası.
        builder.HasOne(a => a.Editor)
            .WithMany()
            .HasForeignKey(a => a.EditorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Translations)
            .WithOne(t => t.Article)
            .HasForeignKey(t => t.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

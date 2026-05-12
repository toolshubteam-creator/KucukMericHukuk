using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("MediaFiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.RelativePath).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ThumbnailRelativePath).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Sha256).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AltText).HasMaxLength(500);

        builder.Property(x => x.IsPublic).HasDefaultValue(false);

        builder.HasIndex(x => x.Sha256).IsUnique();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.IsPublic);

        builder.HasOne(x => x.UploadedBy)
               .WithMany()
               .HasForeignKey(x => x.UploadedByUserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class NewsletterJobConfiguration : IEntityTypeConfiguration<NewsletterJob>
{
    public void Configure(EntityTypeBuilder<NewsletterJob> builder)
    {
        builder.ToTable("NewsletterJobs");
        builder.HasQueryFilter(j => !j.IsDeleted);

        builder.Property(j => j.Status)
            .HasConversion<int>()
            .HasDefaultValue(NewsletterJobStatus.Pending);

        // ErrorSummary için açık tip vermiyoruz — EF provider default'u:
        // SQL Server → nvarchar(max), SQLite → TEXT. Cross-provider testler için zorunlu.

        builder.HasOne(j => j.Article)
            .WithMany()
            .HasForeignKey(j => j.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.ArticleId);
        builder.HasIndex(j => j.CreatedAt);
    }
}

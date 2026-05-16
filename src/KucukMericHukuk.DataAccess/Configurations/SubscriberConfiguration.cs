using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
{
    public void Configure(EntityTypeBuilder<Subscriber> builder)
    {
        builder.ToTable("Subscribers");
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.Property(s => s.Email).IsRequired().HasMaxLength(256);

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .HasDefaultValue(SubscriberStatus.Active);

        builder.Property(s => s.UnsubscribeToken).IsRequired();
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasMaxLength(500);

        // Soft-delete uyumlu unique email — silinmiş kayıt aynı email'i tekrar kaydetmeye engel olmaz.
        builder.HasIndex(s => s.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Subscribers_Email_Unique_NotDeleted");

        builder.HasIndex(s => s.UnsubscribeToken)
            .IsUnique()
            .HasDatabaseName("IX_Subscribers_UnsubscribeToken_Unique");

        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.SubscribedAt);
        builder.HasIndex(s => s.IsDeleted);
    }
}

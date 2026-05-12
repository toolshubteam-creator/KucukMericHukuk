using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class ContactMessageReplyConfiguration : IEntityTypeConfiguration<ContactMessageReply>
{
    public void Configure(EntityTypeBuilder<ContactMessageReply> builder)
    {
        builder.ToTable("ContactMessageReplies");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Body).IsRequired().HasMaxLength(4000);
        builder.Property(r => r.SentAt).IsRequired();

        // Reply parent ContactMessage soft-deleted olsa da query filter ile parent.IsDeleted üzerinden filtrelenir
        // (history korunmalı, sadece UI'da görünmez).
        builder.HasQueryFilter(r => !r.ContactMessage.IsDeleted);

        builder.HasOne(r => r.ContactMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(r => r.ContactMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SentByUser)
            .WithMany()
            .HasForeignKey(r => r.SentByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.ContactMessageId);
        builder.HasIndex(r => r.SentAt);
    }
}

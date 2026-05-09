using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Subject).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Message).IsRequired();
        builder.Property(c => c.IpAddress).HasMaxLength(50);
        builder.Property(c => c.UserAgent).HasMaxLength(500);

        builder.HasIndex(c => c.IsRead);
        builder.HasIndex(c => c.IsDeleted);
        builder.HasIndex(c => c.CreatedAt);
    }
}

using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Email).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Subject).IsRequired().HasMaxLength(200);
        builder.Property(a => a.PreferredDate).IsRequired();
        builder.Property(a => a.PreferredTimeNote).HasMaxLength(100);
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.AdminNote).HasMaxLength(2000);

        // AppointmentStatus enum int olarak persist edilir (ContactMessage'in IsRead/IsAnswered bool'larinin yerini alir).
        builder.Property(a => a.Status).HasConversion<int>();

        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.IsDeleted);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.PreferredDate);
    }
}

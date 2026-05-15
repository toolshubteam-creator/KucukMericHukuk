using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        // Guid PK — DB tarafında NEWID() yerine interceptor Guid.NewGuid() set ediyor (uygulama tarafında üretiliyor).
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.UserName).HasMaxLength(256);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.Action).HasConversion<int>();
        // ChangesJson nvarchar(max) — uzun JSON destekler (Created/Deleted snapshot)

        // Audit kaydı SİLİNMEZ — global query filter YOK (BaseEntity türü değil zaten).

        builder.HasIndex(a => a.CreatedAt).IsDescending();
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.UserId);
    }
}

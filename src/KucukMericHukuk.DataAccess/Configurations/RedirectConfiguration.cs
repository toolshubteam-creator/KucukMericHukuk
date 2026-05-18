using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class RedirectConfiguration : IEntityTypeConfiguration<Redirect>
{
    public void Configure(EntityTypeBuilder<Redirect> builder)
    {
        builder.ToTable("Redirects");
        builder.HasKey(r => r.Id);

        // FromPath NVARCHAR(500) — SQL Server non-clustered key 1700 byte içinde rahat.
        // UNIQUE: aynı path için tek kayıt (aktif veya pasif).
        builder.Property(r => r.FromPath).IsRequired().HasMaxLength(500);
        builder.Property(r => r.ToPath).IsRequired().HasMaxLength(500);

        builder.Property(r => r.StatusCode).HasDefaultValue(301);
        builder.Property(r => r.IsActive).HasDefaultValue(true);
        builder.Property(r => r.HitCount).HasDefaultValue(0);

        builder.HasIndex(r => r.FromPath).IsUnique();
        builder.HasIndex(r => r.IsActive);

        // Redirect BaseEntity türü değil → global query filter (IsDeleted) YOK.
        // Admin "deaktif et" akışı IsActive=false set ile çalışır (delete değil).
        // Hard-delete admin CRUD'unda destekli — audit'e Deleted olarak düşer.
    }
}

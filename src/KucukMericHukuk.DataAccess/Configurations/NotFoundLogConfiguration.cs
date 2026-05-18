using KucukMericHukuk.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class NotFoundLogConfiguration : IEntityTypeConfiguration<NotFoundLog>
{
    public void Configure(EntityTypeBuilder<NotFoundLog> builder)
    {
        builder.ToTable("NotFoundLogs");
        builder.HasKey(n => n.Id);

        // Guid PK uygulama tarafında üretilir (DB'de NEWID() YOK) — AuditLog ile aynı pattern.
        builder.Property(n => n.Id).ValueGeneratedNever();

        // Url NVARCHAR(850): SQL Server non-clustered index key 1700 byte limit (2 byte/char = 850 char).
        // 850 üstü URL'ler middleware'de (Faz 7.3.2) truncate edilir.
        builder.Property(n => n.Url).IsRequired().HasMaxLength(850);

        // Referer/UserAgent index'lenmez — uzun değerler güvenli.
        builder.Property(n => n.Referer).HasMaxLength(2048);
        builder.Property(n => n.UserAgent).HasMaxLength(512);
        builder.Property(n => n.IpAddress).HasMaxLength(45);

        // Aggregate key — race-safe upsert için unique constraint zorunlu.
        // RecordHitAsync DbUpdateException (unique violation) yakalayıp retry-update yapar.
        builder.HasIndex(n => n.Url).IsUnique();

        // Admin liste sıralaması: HitCount DESC (varsayılan) ve LastSeenAt DESC (alternatif).
        builder.HasIndex(n => n.HitCount).IsDescending();
        builder.HasIndex(n => n.LastSeenAt).IsDescending();

        // NotFoundLog BaseEntity türü değil → global query filter (IsDeleted) YOK,
        // soft-delete YOK. Log kaydı silinmez (manuel hard-delete dışında).
    }
}

using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Audit log seti — AuditSaveChangesInterceptor SavingChanges anında bu DbSet üzerinden
    /// kayıt ekler. Repo'lar üzerinden CRUD YOK (AuditLogRepository readonly).
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// 404 hit aggregate seti (Faz 7.3.1) — NotFoundLoggingMiddleware (Faz 7.3.2)
    /// her 404'te NotFoundLogRepository.RecordHitAsync üzerinden upsert eder.
    /// </summary>
    public DbSet<NotFoundLog> NotFoundLogs => Set<NotFoundLog>();

    /// <summary>
    /// Manuel URL yönlendirme seti (Faz 7.4.1). RedirectMiddleware (Faz 7.4.2)
    /// FromPath lookup yapar; admin CRUD ile yönetilir.
    /// </summary>
    public DbSet<Redirect> Redirects => Set<Redirect>();

    /// <summary>
    /// Slug değişim izi (Faz 7.4.1) — 6 servis update (Faz 7.4.3) slug değişince
    /// otomatik kayıt eder; RedirectMiddleware (Faz 7.4.2) eski slug'ı güncele
    /// yönlendirir.
    /// </summary>
    public DbSet<SlugHistory> SlugHistories => Set<SlugHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity tablo isimlerini sadeleştir (AspNet prefix kaldır)
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

        // Configurations assembly'sinden tüm IEntityTypeConfiguration<T>'leri uygula.
        // Soft delete global query filter'ları her BaseEntity türevinin kendi config'inde
        // statik typed lambda olarak tanımlıdır (her configuration: HasQueryFilter(x => !x.IsDeleted)).
        // Translation tabloları parent.IsDeleted üzerinden eşleştirilmiştir.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Modified)
                    baseEntity.UpdatedAt = now;
            }
            else if (entry.Entity is ApplicationUser user)
            {
                if (entry.State == EntityState.Modified)
                    user.UpdatedAt = now;
            }
            else if (entry.Entity is ApplicationRole role)
            {
                if (entry.State == EntityState.Modified)
                    role.UpdatedAt = now;
            }
        }
    }
}

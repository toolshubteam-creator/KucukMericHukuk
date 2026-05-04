using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

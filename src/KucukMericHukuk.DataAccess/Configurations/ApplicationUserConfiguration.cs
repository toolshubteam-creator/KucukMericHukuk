using KucukMericHukuk.Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KucukMericHukuk.DataAccess.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(150);
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.HasIndex(u => u.IsDeleted);
        builder.HasIndex(u => u.IsActive);
    }
}

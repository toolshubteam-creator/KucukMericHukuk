using Microsoft.AspNetCore.Identity;

namespace KucukMericHukuk.Core.Entities.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

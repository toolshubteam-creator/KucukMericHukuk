using Microsoft.AspNetCore.Identity;

namespace KucukMericHukuk.Core.Entities.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Devre dışı bırakılmış kullanıcı. IsActive=false ise login engellenir.
    /// IsDeleted'den farklıdır: IsDeleted "silinmiş kayıt" (uzun vadeli), IsActive "geçici askıya alma".
    /// Faz 6.8'de eklendi.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

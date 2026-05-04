using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities;

public class Attorney : BaseEntity, ITranslatable<AttorneyTranslation>
{
    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string? BarRegistrationNumber { get; set; }
    public string? BarName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<AttorneyTranslation> Translations { get; set; } = new List<AttorneyTranslation>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}

namespace KucukMericHukuk.Core.Entities;

public class ContactMessage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool KvkkConsent { get; set; }
    public bool IsRead { get; set; }
    public bool IsAnswered { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

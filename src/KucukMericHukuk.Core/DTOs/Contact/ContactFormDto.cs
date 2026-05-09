namespace KucukMericHukuk.Core.DTOs.Contact;

public class ContactFormDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool KvkkConsent { get; set; }

    // Honeypot (bot trap, gerçek kullanıcı görmez, doluysa silent reject)
    public string? Website { get; set; }

    // Audit
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

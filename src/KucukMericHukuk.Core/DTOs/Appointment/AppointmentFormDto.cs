namespace KucukMericHukuk.Core.DTOs.Appointment;

public class AppointmentFormDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateOnly? PreferredDate { get; set; }
    public string? PreferredTimeNote { get; set; }
    public string? Notes { get; set; }
    public bool KvkkConsent { get; set; }

    // Honeypot (bot trap, gercek kullanici gormez, doluysa silent reject)
    public string? Website { get; set; }

    // Audit
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

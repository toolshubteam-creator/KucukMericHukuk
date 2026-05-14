using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Appointment;

public class AppointmentAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateOnly PreferredDate { get; set; }
    public string? PreferredTimeNote { get; set; }
    public string? Notes { get; set; }
    public bool KvkkConsent { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public bool IsDeleted { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

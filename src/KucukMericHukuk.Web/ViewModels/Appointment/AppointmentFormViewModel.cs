namespace KucukMericHukuk.Web.ViewModels.Appointment;

public class AppointmentFormViewModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public string? PreferredTimeNote { get; set; }
    public string? Notes { get; set; }
    public bool KvkkConsent { get; set; }
    public string? Website { get; set; } // honeypot

    /// <summary>Cloudflare Turnstile token (Faz 5.6). Widget data-response-field-name="TurnstileToken" ile bind olur.</summary>
    public string? TurnstileToken { get; set; }
}

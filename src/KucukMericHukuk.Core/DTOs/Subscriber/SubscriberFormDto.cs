namespace KucukMericHukuk.Core.DTOs.Subscriber;

public class SubscriberFormDto
{
    public string Email { get; set; } = string.Empty;
    public bool KvkkConsent { get; set; }

    /// <summary>Honeypot — bot doldurursa service silent success ile DB'ye yazmaz.</summary>
    public string? Website { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

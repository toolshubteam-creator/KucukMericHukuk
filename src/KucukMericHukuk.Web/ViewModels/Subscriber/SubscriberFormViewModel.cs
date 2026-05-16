namespace KucukMericHukuk.Web.ViewModels.Subscriber;

public class SubscriberFormViewModel
{
    public string? Email { get; set; }
    public bool KvkkConsent { get; set; }

    /// <summary>Honeypot — bot doldurursa service silent success ile DB'ye yazmaz.</summary>
    public string? Website { get; set; }

    /// <summary>Cloudflare Turnstile token — widget data-response-field-name="TurnstileToken" ile bind olur.</summary>
    public string? TurnstileToken { get; set; }
}

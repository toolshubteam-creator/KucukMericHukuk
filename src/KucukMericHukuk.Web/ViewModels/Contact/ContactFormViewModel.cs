namespace KucukMericHukuk.Web.ViewModels.Contact;

public class ContactFormViewModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public bool KvkkConsent { get; set; }
    public string? Website { get; set; } // honeypot
}

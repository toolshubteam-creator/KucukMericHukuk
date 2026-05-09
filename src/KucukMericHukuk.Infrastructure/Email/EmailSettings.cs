namespace KucukMericHukuk.Infrastructure.Email;

public class EmailSettings
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "noreply@kucukmerichukuk.av.tr";
    public string FromName { get; set; } = "Küçükmeriç Hukuk Bürosu";
    public string? AdminNotificationEmail { get; set; }
}

using KucukMericHukuk.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KucukMericHukuk.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;

            var body = new BodyBuilder { HtmlBody = htmlBody };
            msg.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            // SmtpEmailSender DI'da yalnızca SmtpHost non-empty iken register edilir; null değildir.
            await client.ConnectAsync(_settings.SmtpHost!, _settings.SmtpPort,
                _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, ct);
            }

            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {To}, subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            // Form submit user-facing fail olmamalı; mail eksik kalsın, log yetsin.
            _logger.LogError(ex, "Email gönderimi başarısız. To: {To}", to);
        }
    }
}

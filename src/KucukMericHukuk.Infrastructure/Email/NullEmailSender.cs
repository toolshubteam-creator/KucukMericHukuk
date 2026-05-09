using KucukMericHukuk.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Infrastructure.Email;

public class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;

    public NullEmailSender(ILogger<NullEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("[NullEmailSender] To={To} | Subject={Subject} | Body length={BodyLen}",
            to, subject, htmlBody?.Length ?? 0);
        return Task.CompletedTask;
    }
}

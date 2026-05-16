using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace KucukMericHukuk.IntegrationTests.Newsletter;

/// <summary>
/// Faz 7.2b-2: SendJob integration testleri için IEmailSender'ı Mock ile override eden factory.
/// Default factory NullEmailSender kullanıyor; testlerde gönderim çağrı sayısını/argümanını
/// doğrulamak için Mock'a ihtiyaç var.
/// </summary>
public class NewsletterSendTestFactory : IntegrationTestFactory
{
    public Mock<IEmailSender> EmailMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Replace IEmailSender — default impl (Null/Smtp) yerine Mock
            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender>(_ => EmailMock.Object);
        });
    }
}

using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.FileStorage;
using KucukMericHukuk.Infrastructure.Initialization;
using KucukMericHukuk.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<IDbInitializer, DbInitializer>();

        // HtmlSanitizer thread-safe (paket dokümantasyonu) — Singleton
        services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();

        // Medya: dosya saklama + görsel işleme
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IImageProcessor, SkiaSharpProcessor>();

        return services;
    }
}

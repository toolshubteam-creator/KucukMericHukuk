using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.FileStorage;
using KucukMericHukuk.Infrastructure.Google;
using KucukMericHukuk.Infrastructure.Initialization;
using KucukMericHukuk.Infrastructure.Security;
using KucukMericHukuk.Infrastructure.Seo;
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
        services.Configure<GoogleIntegrationOptions>(
            configuration.GetSection(GoogleIntegrationOptions.SectionName));
        services.AddScoped<IDbInitializer, DbInitializer>();

        // HtmlSanitizer thread-safe (paket dokümantasyonu) — Singleton
        services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();

        // Medya: dosya saklama + görsel işleme
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IImageProcessor, SkiaSharpProcessor>();

        // SEO: JSON-LD üreticisi (SiteInfoOptions Web katmanında register, burada IOptions ile çözülür)
        services.AddScoped<IJsonLdService, JsonLdService>();

        // SEO: sitemap.xml + robots.txt üreticisi
        services.AddScoped<ISitemapService, SitemapService>();

        // Cloudflare Turnstile bot doğrulama (Faz 5.6) — typed HttpClient
        services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>();

        // Google API ortak client (Faz 7.5). Credential env/config veya SiteSettings'ten okunur.
        services.AddScoped<IGoogleApiClient, GoogleApiClient>();
        services.AddScoped<IAnalyticsDataReportClient, AnalyticsDataReportClient>();
        services.AddScoped<IGoogleAnalyticsService, GoogleAnalyticsService>();
        services.AddScoped<ISearchConsoleReportClient, SearchConsoleReportClient>();
        services.AddScoped<IGoogleSearchConsoleService, GoogleSearchConsoleService>();

        return services;
    }
}

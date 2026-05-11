using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Configuration;

/// <summary>
/// SiteInfoOptions'ı DB'deki SiteSettings tablosundan hydrate eder. Zincirleme:
/// önce appsettings.json (Program.cs içindeki Configure) bind olur, sonra burası override eder.
/// Tüketiciler IOptionsSnapshot&lt;SiteInfoOptions&gt; kullandığı için bu Configure her request
/// scope'unda yeniden çalışır; SiteSettingsService kendi 5dk memory-cache'iyle DB hit'i sınırlar.
/// Admin update sonrası SiteSettingsService.InvalidateCache() çağrılır → bir sonraki request fresh DB okur.
/// </summary>
public class SiteInfoOptionsConfigurator : IConfigureOptions<SiteInfoOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SiteInfoOptionsConfigurator> _logger;

    public SiteInfoOptionsConfigurator(
        IServiceScopeFactory scopeFactory,
        ILogger<SiteInfoOptionsConfigurator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Configure(SiteInfoOptions options)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ISiteSettingsService>();
            var result = settingsService.GetAllAsync().GetAwaiter().GetResult();
            if (result.IsFailure || result.Value is null) return;

            var dict = result.Value;
            ApplyIfPresent(dict, SiteSettingKeys.Name, v => options.Name = v);
            ApplyIfPresent(dict, SiteSettingKeys.Tagline, v => options.Tagline = v);
            ApplyIfPresent(dict, SiteSettingKeys.Description, v => options.Description = v);
            ApplyIfPresent(dict, SiteSettingKeys.BaseUrl, v => options.BaseUrl = v);
            ApplyIfPresent(dict, SiteSettingKeys.DefaultOgImage, v => options.DefaultOgImage = v);
            ApplyIfPresent(dict, SiteSettingKeys.Locale, v => options.Locale = v);
            ApplyIfPresent(dict, SiteSettingKeys.TwitterHandle, v => options.TwitterHandle = v);
            ApplyIfPresent(dict, SiteSettingKeys.Telephone, v => options.Telephone = v);
            ApplyIfPresent(dict, SiteSettingKeys.Email, v => options.Email = v);
            ApplyIfPresent(dict, SiteSettingKeys.StreetAddress, v => options.StreetAddress = v);
            ApplyIfPresent(dict, SiteSettingKeys.AddressLocality, v => options.AddressLocality = v);
            ApplyIfPresent(dict, SiteSettingKeys.AddressRegion, v => options.AddressRegion = v);
            ApplyIfPresent(dict, SiteSettingKeys.PostalCode, v => options.PostalCode = v);
            ApplyIfPresent(dict, SiteSettingKeys.AddressCountry, v => options.AddressCountry = v);
            ApplyIfPresent(dict, SiteSettingKeys.AreaServed, v => options.AreaServed = v);
            ApplyIfPresent(dict, SiteSettingKeys.OpeningHoursDescription, v => options.OpeningHoursDescription = v);

            if (dict.TryGetValue(SiteSettingKeys.Latitude, out var lat) &&
                decimal.TryParse(lat, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var latDec))
            {
                options.Latitude = (double)latDec;
            }
            if (dict.TryGetValue(SiteSettingKeys.Longitude, out var lng) &&
                decimal.TryParse(lng, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var lngDec))
            {
                options.Longitude = (double)lngDec;
            }
        }
        catch (Exception ex)
        {
            // DB henüz hazır değilse (ilk migration, design-time) appsettings fallback'i bozma
            _logger.LogWarning(ex, "SiteInfoOptions DB hydration başarısız — appsettings fallback kullanılıyor.");
        }
    }

    private static void ApplyIfPresent(IReadOnlyDictionary<string, string?> dict, string key, Action<string> setter)
    {
        if (dict.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            setter(value);
        }
    }
}

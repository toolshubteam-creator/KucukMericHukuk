using Google.Apis.SearchConsole.v1;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Google;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Google;

public class GoogleSearchConsoleService : IGoogleSearchConsoleService
{
    private static readonly string[] SearchConsoleScopes =
    [
        SearchConsoleService.Scope.WebmastersReadonly
    ];

    private readonly ISiteSettingsService _siteSettings;
    private readonly IGoogleApiClient _googleApiClient;
    private readonly ISearchConsoleReportClient _searchConsoleReportClient;
    private readonly GoogleIntegrationOptions _options;
    private readonly ILogger<GoogleSearchConsoleService> _logger;

    public GoogleSearchConsoleService(
        ISiteSettingsService siteSettings,
        IGoogleApiClient googleApiClient,
        ISearchConsoleReportClient searchConsoleReportClient,
        IOptions<GoogleIntegrationOptions> options,
        ILogger<GoogleSearchConsoleService> logger)
    {
        _siteSettings = siteSettings;
        _googleApiClient = googleApiClient;
        _searchConsoleReportClient = searchConsoleReportClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<GoogleSearchConsoleDashboardWidgetDto>> GetDashboardWidgetAsync(
        CancellationToken ct = default)
    {
        var siteUrl = await GetSiteUrlAsync(ct);
        if (string.IsNullOrWhiteSpace(siteUrl))
            return Result.Success(NotConfigured("Search Console Site URL yapılandırılmamış."));

        var tokenResult = await _googleApiClient.GetAccessTokenAsync(SearchConsoleScopes, ct);
        if (tokenResult.IsFailure)
        {
            return Result.Success(NotConfigured(
                tokenResult.FirstError?.Message ?? "Google API credential yapılandırılmamış.",
                siteUrl));
        }

        try
        {
            var summary = await _searchConsoleReportClient.GetSummaryAsync(
                tokenResult.Value,
                siteUrl,
                _options.ApplicationName,
                ct);

            return Result.Success(new GoogleSearchConsoleDashboardWidgetDto
            {
                IsConfigured = true,
                HasData = summary.Clicks > 0 || summary.Impressions > 0,
                SiteUrl = siteUrl,
                Message = "Search Console verisi alındı.",
                Clicks = summary.Clicks,
                Impressions = summary.Impressions,
                CtrPercent = Math.Round(summary.Ctr * 100, 2),
                AveragePosition = Math.Round(summary.AveragePosition, 1),
                FetchedAtUtc = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search Console API raporu alınamadı. SiteUrl={SiteUrl}", siteUrl);
            return Result.Success(new GoogleSearchConsoleDashboardWidgetDto
            {
                IsConfigured = true,
                SiteUrl = siteUrl,
                Message = "Search Console verisi alınamadı. Site URL, service account yetkisi ve API erişimini kontrol edin.",
            });
        }
    }

    private async Task<string?> GetSiteUrlAsync(CancellationToken ct)
    {
        var result = await _siteSettings.GetValueAsync(SiteSettingKeys.GoogleSearchConsoleSiteUrl, ct);
        return result.IsSuccess ? result.Value?.Trim() : null;
    }

    private static GoogleSearchConsoleDashboardWidgetDto NotConfigured(string message, string? siteUrl = null) =>
        new()
        {
            IsConfigured = false,
            SiteUrl = siteUrl,
            Message = message,
        };
}

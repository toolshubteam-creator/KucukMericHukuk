using Google.Apis.AnalyticsData.v1beta;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Google;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Google;

public class GoogleAnalyticsService : IGoogleAnalyticsService
{
    private static readonly string[] AnalyticsScopes =
    [
        AnalyticsDataService.Scope.AnalyticsReadonly
    ];

    private readonly ISiteSettingsService _siteSettings;
    private readonly IGoogleApiClient _googleApiClient;
    private readonly IAnalyticsDataReportClient _analyticsDataReportClient;
    private readonly GoogleIntegrationOptions _options;
    private readonly ILogger<GoogleAnalyticsService> _logger;

    public GoogleAnalyticsService(
        ISiteSettingsService siteSettings,
        IGoogleApiClient googleApiClient,
        IAnalyticsDataReportClient analyticsDataReportClient,
        IOptions<GoogleIntegrationOptions> options,
        ILogger<GoogleAnalyticsService> logger)
    {
        _siteSettings = siteSettings;
        _googleApiClient = googleApiClient;
        _analyticsDataReportClient = analyticsDataReportClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<GoogleAnalyticsDashboardWidgetDto>> GetDashboardWidgetAsync(
        CancellationToken ct = default)
    {
        var propertyId = await GetPropertyIdAsync(ct);
        if (string.IsNullOrWhiteSpace(propertyId))
            return Result.Success(NotConfigured("GA4 Property ID yapılandırılmamış."));

        var tokenResult = await _googleApiClient.GetAccessTokenAsync(AnalyticsScopes, ct);
        if (tokenResult.IsFailure)
        {
            return Result.Success(NotConfigured(
                tokenResult.FirstError?.Message ?? "Google API credential yapılandırılmamış.",
                propertyId));
        }

        try
        {
            var summary = await _analyticsDataReportClient.GetSummaryAsync(
                tokenResult.Value,
                ToPropertyResourceName(propertyId),
                _options.ApplicationName,
                ct);

            return Result.Success(new GoogleAnalyticsDashboardWidgetDto
            {
                IsConfigured = true,
                HasData = summary.ActiveUsers > 0 ||
                          summary.NewUsers > 0 ||
                          summary.Sessions > 0 ||
                          summary.ScreenPageViews > 0 ||
                          summary.EventCount > 0,
                PropertyId = propertyId,
                Message = "GA4 verisi alındı.",
                ActiveUsers = summary.ActiveUsers,
                NewUsers = summary.NewUsers,
                Sessions = summary.Sessions,
                ScreenPageViews = summary.ScreenPageViews,
                EventCount = summary.EventCount,
                FetchedAtUtc = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GA4 Data API raporu alınamadı. PropertyId={PropertyId}", propertyId);
            return Result.Success(new GoogleAnalyticsDashboardWidgetDto
            {
                IsConfigured = true,
                PropertyId = propertyId,
                Message = "GA4 verisi alınamadı. Property ID, service account yetkisi ve API erişimini kontrol edin.",
            });
        }
    }

    private async Task<string?> GetPropertyIdAsync(CancellationToken ct)
    {
        var result = await _siteSettings.GetValueAsync(SiteSettingKeys.GoogleAnalyticsPropertyId, ct);
        return result.IsSuccess ? result.Value?.Trim() : null;
    }

    private static GoogleAnalyticsDashboardWidgetDto NotConfigured(string message, string? propertyId = null) =>
        new()
        {
            IsConfigured = false,
            PropertyId = propertyId,
            Message = message,
        };

    private static string ToPropertyResourceName(string propertyId)
    {
        var trimmed = propertyId.Trim();
        return trimmed.StartsWith("properties/", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"properties/{trimmed}";
    }
}

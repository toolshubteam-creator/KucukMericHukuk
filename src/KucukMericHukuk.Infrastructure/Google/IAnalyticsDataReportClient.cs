namespace KucukMericHukuk.Infrastructure.Google;

public interface IAnalyticsDataReportClient
{
    Task<AnalyticsDataSummary> GetSummaryAsync(
        string accessToken,
        string propertyResourceName,
        string applicationName,
        CancellationToken ct);
}

public sealed record AnalyticsDataSummary(
    long ActiveUsers,
    long NewUsers,
    long Sessions,
    long ScreenPageViews,
    long EventCount);

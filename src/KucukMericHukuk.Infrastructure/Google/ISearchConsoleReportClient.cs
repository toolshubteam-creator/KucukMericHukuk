namespace KucukMericHukuk.Infrastructure.Google;

public interface ISearchConsoleReportClient
{
    Task<SearchConsoleSummary> GetSummaryAsync(
        string accessToken,
        string siteUrl,
        string applicationName,
        CancellationToken ct);
}

public sealed record SearchConsoleSummary(
    long Clicks,
    long Impressions,
    double Ctr,
    double AveragePosition);

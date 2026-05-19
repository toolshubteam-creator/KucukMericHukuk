using Google.Apis.Auth.OAuth2;
using Google.Apis.SearchConsole.v1;
using Google.Apis.SearchConsole.v1.Data;
using Google.Apis.Services;

namespace KucukMericHukuk.Infrastructure.Google;

public class SearchConsoleReportClient : ISearchConsoleReportClient
{
    public async Task<SearchConsoleSummary> GetSummaryAsync(
        string accessToken,
        string siteUrl,
        string applicationName,
        CancellationToken ct)
    {
        using var service = new SearchConsoleService(new BaseClientService.Initializer
        {
            ApplicationName = applicationName,
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
        });

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var start = yesterday.AddDays(-27);
        var request = new SearchAnalyticsQueryRequest
        {
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = yesterday.ToString("yyyy-MM-dd"),
            SearchType = "web",
            RowLimit = 1,
        };

        var response = await service.Searchanalytics.Query(request, siteUrl).ExecuteAsync(ct);
        var row = response.Rows?.FirstOrDefault();

        return new SearchConsoleSummary(
            Clicks: Convert.ToInt64(row?.Clicks ?? 0),
            Impressions: Convert.ToInt64(row?.Impressions ?? 0),
            Ctr: row?.Ctr ?? 0,
            AveragePosition: row?.Position ?? 0);
    }
}

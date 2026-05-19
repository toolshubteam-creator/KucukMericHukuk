using System.Globalization;
using Google.Apis.AnalyticsData.v1beta;
using Google.Apis.AnalyticsData.v1beta.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;

namespace KucukMericHukuk.Infrastructure.Google;

public class AnalyticsDataReportClient : IAnalyticsDataReportClient
{
    public async Task<AnalyticsDataSummary> GetSummaryAsync(
        string accessToken,
        string propertyResourceName,
        string applicationName,
        CancellationToken ct)
    {
        using var service = new AnalyticsDataService(new BaseClientService.Initializer
        {
            ApplicationName = applicationName,
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
        });

        var request = new RunReportRequest
        {
            DateRanges = new List<DateRange>
            {
                new() { StartDate = "28daysAgo", EndDate = "yesterday" },
            },
            Metrics = new List<Metric>
            {
                new() { Name = "activeUsers" },
                new() { Name = "newUsers" },
                new() { Name = "sessions" },
                new() { Name = "screenPageViews" },
                new() { Name = "eventCount" },
            },
        };

        var response = await service.Properties.RunReport(request, propertyResourceName).ExecuteAsync(ct);
        var values = response.Rows?.FirstOrDefault()?.MetricValues;

        return new AnalyticsDataSummary(
            ParseMetric(values, 0),
            ParseMetric(values, 1),
            ParseMetric(values, 2),
            ParseMetric(values, 3),
            ParseMetric(values, 4));
    }

    private static long ParseMetric(IList<MetricValue>? values, int index)
    {
        if (values is null || values.Count <= index)
            return 0;

        return long.TryParse(values[index].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }
}

namespace KucukMericHukuk.Core.DTOs.Google;

public class GoogleSearchConsoleDashboardWidgetDto
{
    public bool IsConfigured { get; init; }
    public bool HasData { get; init; }
    public string? SiteUrl { get; init; }
    public string DateRangeLabel { get; init; } = "Son 28 gün";
    public string Message { get; init; } = "Search Console API yapılandırılmamış.";
    public long Clicks { get; init; }
    public long Impressions { get; init; }
    public double CtrPercent { get; init; }
    public double AveragePosition { get; init; }
    public DateTime? FetchedAtUtc { get; init; }
}

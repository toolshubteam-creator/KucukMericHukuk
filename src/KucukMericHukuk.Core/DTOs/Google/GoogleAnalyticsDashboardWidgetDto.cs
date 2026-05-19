namespace KucukMericHukuk.Core.DTOs.Google;

public class GoogleAnalyticsDashboardWidgetDto
{
    public bool IsConfigured { get; init; }
    public bool HasData { get; init; }
    public string? PropertyId { get; init; }
    public string DateRangeLabel { get; init; } = "Son 28 gün";
    public string Message { get; init; } = "GA4 Data API yapılandırılmamış.";
    public long ActiveUsers { get; init; }
    public long NewUsers { get; init; }
    public long Sessions { get; init; }
    public long ScreenPageViews { get; init; }
    public long EventCount { get; init; }
    public DateTime? FetchedAtUtc { get; init; }
}

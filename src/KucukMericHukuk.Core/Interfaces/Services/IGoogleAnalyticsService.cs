using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Google;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IGoogleAnalyticsService
{
    Task<Result<GoogleAnalyticsDashboardWidgetDto>> GetDashboardWidgetAsync(CancellationToken ct = default);
}

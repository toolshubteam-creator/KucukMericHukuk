using KucukMericHukuk.Core.DTOs.Dashboard;
using KucukMericHukuk.Core.DTOs.Google;

namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// Admin Dashboard widget'larının veri kaynağı (Faz 6.21). Salt-okuma —
/// ContactMessageService'in dashboard widget metotlarıyla aynı rol.
/// </summary>
public interface IDashboardService
{
    /// <summary>"Son Makaleler" widget: toplam makale sayısı + son N yayınlanan makale.</summary>
    Task<RecentArticlesWidgetDto> GetRecentArticlesAsync(
        string languageCode, int count, CancellationToken ct = default);

    /// <summary>"Site Özeti" widget: 7 içerik entity'sinin aktif kayıt sayıları.</summary>
    Task<SiteSummaryWidgetDto> GetSiteSummaryAsync(CancellationToken ct = default);

    Task<GoogleAnalyticsDashboardWidgetDto> GetGoogleAnalyticsWidgetAsync(CancellationToken ct = default);
}

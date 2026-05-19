using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.DTOs.Dashboard;
using KucukMericHukuk.Core.DTOs.Google;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Dashboard;

public class DashboardViewModel
{
    public ContactDashboardWidgetDto Messages { get; init; } = new();
    public RecentArticlesWidgetDto Articles { get; init; } = new();
    public SiteSummaryWidgetDto SiteSummary { get; init; } = new();
    public GoogleAnalyticsDashboardWidgetDto GoogleAnalytics { get; init; } = new();
}

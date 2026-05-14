using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class AdminController : Controller
{
    private const int DashboardRecentCount = 5;

    private readonly IContactMessageService _contactMessageService;
    private readonly IDashboardService _dashboardService;

    public AdminController(
        IContactMessageService contactMessageService,
        IDashboardService dashboardService)
    {
        _contactMessageService = contactMessageService;
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Kontrol Paneli";

        var unreadCount = await _contactMessageService.GetUnreadCountAsync(ct);
        var recent = await _contactMessageService.GetRecentAsync(DashboardRecentCount, ct);
        var articlesWidget = await _dashboardService.GetRecentArticlesAsync(
            LanguageCodes.Default, DashboardRecentCount, ct);
        var siteSummary = await _dashboardService.GetSiteSummaryAsync(ct);

        var vm = new DashboardViewModel
        {
            Messages = new ContactDashboardWidgetDto
            {
                UnreadCount = unreadCount,
                RecentMessages = recent,
            },
            Articles = articlesWidget,
            SiteSummary = siteSummary,
        };

        return View(vm);
    }
}

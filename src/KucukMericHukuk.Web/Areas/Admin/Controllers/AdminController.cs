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
    private readonly IContactMessageService _contactMessageService;

    public AdminController(IContactMessageService contactMessageService)
    {
        _contactMessageService = contactMessageService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Kontrol Paneli";

        var unreadCount = await _contactMessageService.GetUnreadCountAsync(ct);
        var recent = await _contactMessageService.GetRecentAsync(5, ct);

        var vm = new DashboardViewModel
        {
            Messages = new ContactDashboardWidgetDto
            {
                UnreadCount = unreadCount,
                RecentMessages = recent,
            }
        };

        return View(vm);
    }
}

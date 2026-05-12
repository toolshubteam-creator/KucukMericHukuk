using KucukMericHukuk.Core.DTOs.Contact;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Dashboard;

public class DashboardViewModel
{
    public ContactDashboardWidgetDto Messages { get; init; } = new();
}

using Microsoft.AspNetCore.Mvc.Rendering;

namespace KucukMericHukuk.Web.Helpers;

public static class SidebarHelpers
{
    public static string IsActiveController(this IHtmlHelper htmlHelper, string controllerName)
    {
        var currentController = htmlHelper.ViewContext.RouteData.Values["controller"]?.ToString();
        return string.Equals(currentController, controllerName, StringComparison.OrdinalIgnoreCase)
            ? "active"
            : string.Empty;
    }
}

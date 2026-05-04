using KucukMericHukuk.Core.Constants;

namespace KucukMericHukuk.Web.Localization;

public class CultureRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is null)
            return false;

        var culture = value.ToString();
        if (string.IsNullOrEmpty(culture))
            return false;

        return LanguageCodes.Supported.Contains(culture, StringComparer.OrdinalIgnoreCase);
    }
}

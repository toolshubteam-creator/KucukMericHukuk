using KucukMericHukuk.Core.Constants;

namespace KucukMericHukuk.Web.Localization;

public class TurkishCultureRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        return values.TryGetValue(routeKey, out var value)
            && value is not null
            && string.Equals(value.ToString(), LanguageCodes.Turkish, StringComparison.OrdinalIgnoreCase);
    }
}

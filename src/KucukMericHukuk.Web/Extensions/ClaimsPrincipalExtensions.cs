using System.Security.Claims;

namespace KucukMericHukuk.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserIdOrNull(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : null;
    }
}

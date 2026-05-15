using System.Security.Claims;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Web.Infrastructure;

/// <summary>
/// Faz 7.1: <see cref="ICurrentUserAccessor"/>'in HTTP request-bound implementation'ı.
/// HttpContext null ise (background job, migration, test) tüm property'ler null döner.
/// </summary>
public class HttpCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;
            if (http is null) return null;
            var claim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

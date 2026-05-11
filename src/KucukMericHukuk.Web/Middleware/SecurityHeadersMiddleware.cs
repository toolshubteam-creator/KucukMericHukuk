namespace KucukMericHukuk.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://challenges.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net data:; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://challenges.cloudflare.com; " +
        "frame-src https://challenges.cloudflare.com; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

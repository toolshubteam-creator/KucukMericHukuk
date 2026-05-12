using Microsoft.Extensions.Hosting;

namespace KucukMericHukuk.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _contentSecurityPolicy;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _contentSecurityPolicy = BuildCsp(env.IsDevelopment());
    }

    private static string BuildCsp(bool isDevelopment)
    {
        // connect-src: fetch/XHR/WebSocket destinations
        // - Production: 'self' + Turnstile + jsdelivr (source map fetch'leri, Faz 6.8 logout dropdown fix'i)
        // - Development: ek olarak ws://localhost:* (BrowserLink + browser-refresh) + http(s)://localhost:*
        var connectSrc = "'self' https://challenges.cloudflare.com https://cdn.jsdelivr.net";
        if (isDevelopment)
        {
            connectSrc += " ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*";
        }

        return
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://challenges.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net data:; " +
            "img-src 'self' data: https:; " +
            $"connect-src {connectSrc}; " +
            "frame-src https://challenges.cloudflare.com; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';";
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
            headers["Content-Security-Policy"] = _contentSecurityPolicy;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

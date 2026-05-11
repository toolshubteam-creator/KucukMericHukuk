using Microsoft.AspNetCore.Diagnostics;

namespace KucukMericHukuk.Web.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Development'ta DeveloperExceptionPage'in göstermesi için tekrar fırlat
            if (_env.IsDevelopment())
            {
                throw;
            }

            _logger.LogError(ex,
                "Unhandled exception. Path={Path} IP={Ip} User={User}",
                context.Request.Path,
                context.Connection.RemoteIpAddress?.ToString(),
                context.User?.Identity?.Name ?? "(anonymous)");

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
                {
                    Error = ex,
                    Path = context.Request.Path
                });

                // UseStatusCodePagesWithReExecute /tr-TR/Error/500'e yönlendirir
            }
        }
    }

    private sealed class ExceptionHandlerFeature : IExceptionHandlerFeature, IExceptionHandlerPathFeature
    {
        public Exception Error { get; set; } = default!;
        public string Path { get; set; } = string.Empty;
    }
}

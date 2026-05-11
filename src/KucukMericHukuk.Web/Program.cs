using System.Globalization;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using KucukMericHukuk.Business;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.DataAccess;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Infrastructure;
using KucukMericHukuk.Infrastructure.Email;
using KucukMericHukuk.Infrastructure.Initialization;
using KucukMericHukuk.Web;
using KucukMericHukuk.Web.Areas.Admin.Identity;
using KucukMericHukuk.Web.Localization;
using KucukMericHukuk.Web.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// DbContext (Testing ortamında entegrasyon testleri kendi provider'ını ekliyor)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false; // Adım 5'te email confirmation eklenecek
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Identity'nin default claims factory'sini override et — FullName claim'ini ekler
builder.Services.AddScoped<
    IUserClaimsPrincipalFactory<ApplicationUser>,
    AppUserClaimsPrincipalFactory>();

// Cookie auth (Identity'nin application cookie'si)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "KucukMericHukuk.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.LoginPath = "/admin/account/login";
    options.LogoutPath = "/admin/account/logout";
    options.AccessDeniedPath = "/admin/error/access-denied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    options.ReturnUrlParameter = "returnUrl";
});

// Rate limiting (Faz 5.7) — 3 named policy + global IP-bazlı limiter
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts)
            ? (int)ts.TotalSeconds
            : 60;

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            $"{{\"error\":\"rate-limit-exceeded\",\"retryAfter\":{retryAfter}}}", ct);
    };

    options.AddPolicy("contact-form", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("admin-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    static string GetClientIp(HttpContext ctx)
        => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
});

// Repository + UnitOfWork
builder.Services.AddDataAccess();

// Infrastructure (SeedOptions + DbInitializer)
builder.Services.AddInfrastructure(builder.Configuration);

// Site bilgileri (SEO meta tags, sitemap üretimi vb. için ortak config)
// Önce appsettings.json'dan bind, sonra DB'den hydrate (SiteInfoOptionsConfigurator)
builder.Services.Configure<SiteInfoOptions>(
    builder.Configuration.GetSection(SiteInfoOptions.SectionName));
builder.Services.AddSingleton<IConfigureOptions<SiteInfoOptions>,
    KucukMericHukuk.Infrastructure.Configuration.SiteInfoOptionsConfigurator>();

// Cloudflare Turnstile (Contact form bot koruması, Faz 5.6)
builder.Services.Configure<TurnstileOptions>(
    builder.Configuration.GetSection(TurnstileOptions.SectionName));

// Email (SmtpHost doluysa SmtpEmailSender, boşsa NullEmailSender — dev fallback)
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
var smtpHost = builder.Configuration["EmailSettings:SmtpHost"];
if (!string.IsNullOrWhiteSpace(smtpHost))
{
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, NullEmailSender>();
}

// Mapster + Business services
builder.Services.AddBusiness();

// Web katmanindaki Mapster IRegister'lari (PageFormMappingConfig vb.) GlobalSettings'e ekle
builder.Services.AddWebMappings();

// FormViewModel-level FluentValidation validator'ları (Page/Tag/Service/Category/
// Attorney). Client-side data-val-* attribute üretimi için kayıt edilir.
builder.Services.AddWebValidators();

// FluentValidation client-side adapter (jQuery unobtrusive validation için
// FluentValidation kurallarının HTML data-val-* attribute'larına yansıması).
builder.Services.AddFluentValidationClientsideAdapters();

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = LanguageCodes.Supported
        .Select(c => new CultureInfo(c))
        .ToList();

    options.DefaultRequestCulture = new RequestCulture(LanguageCodes.Default);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Culture algılama önceliği: Route → Cookie → Accept-Language
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new RouteDataRequestCultureProvider
    {
        RouteDataStringKey = "culture",
        UIRouteDataStringKey = "culture"
    });
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// Route constraint
builder.Services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap.Add("culture", typeof(CultureRouteConstraint));
});

// MVC + Localization
builder.Services.AddControllersWithViews(options =>
    {
        // FluentValidation NotEmpty kuralları zaten Türkçe mesaj sağlıyor;
        // MVC'nin non-nullable string'lere implicit [Required] eklemesi
        // duplicate "The X field is required." mesajı üretiyor — kapatıldı.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// Security headers (Faz 5.7) — EN ÜST (her response'a uygulanır)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Global exception handler (Faz 5.7) — production'da unhandled'ı yakalar, structured log
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Rate limiter (Faz 5.7) — routing'ten SONRA, auth'tan ÖNCE
app.UseRateLimiter();

// Localization middleware: routing'ten SONRA, auth'tan ÖNCE
var localizationOptions = app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

// 4xx/5xx yakalayıp culture-aware Error sayfasına yönlendir.
// Sabit "tr-TR" — çok dilli destek genişlerse middleware'in RouteData'dan
// culture'ı okuması gerekir (DEFERRED Faz 5/6).
app.UseStatusCodePagesWithReExecute("/tr-TR/Error/{0}");

app.UseAuthentication();
app.UseAuthorization();

// Kök URL → default culture'a redirect
app.MapGet("/", context =>
{
    context.Response.Redirect($"/{LanguageCodes.Default}", permanent: false);
    return Task.CompletedTask;
});

// Default route — culture prefix zorunlu, geçersiz culture'da 404
app.MapControllerRoute(
    name: "default",
    pattern: "{culture:culture}/{controller=Home}/{action=Index}/{id?}");

// Admin Area route — culture-bağımsız (sadece TR olacak şimdilik)
app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "admin/{controller=Admin}/{action=Index}/{id?}");

// DbInitializer (Testing ortamında çalıştırma — testler izole DB kullanıyor)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await initializer.InitializeAsync();
}

app.Run();

public partial class Program { }

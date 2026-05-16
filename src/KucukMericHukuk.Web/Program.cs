using System.Globalization;
using System.IO.Compression;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using KucukMericHukuk.Business;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.DataAccess;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Interceptors;
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
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// DbContext (Testing ortamında entegrasyon testleri kendi provider'ını ekliyor)
// Faz 7.1: IHttpContextAccessor + ICurrentUserAccessor — AuditSaveChangesInterceptor user + IP almak için.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<KucukMericHukuk.Core.Interfaces.ICurrentUserAccessor,
    KucukMericHukuk.Web.Infrastructure.HttpCurrentUserAccessor>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        options
            .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
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
// Test ortamında eşikler büyütülür (tek IP'den çoklu test → limit aşımı yaşanmasın).
var rateLimitMultiplier = builder.Environment.IsEnvironment("Testing") ? 1000 : 1;
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
                PermitLimit = 5 * rateLimitMultiplier,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("subscribe-form", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5 * rateLimitMultiplier,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("appointment-form", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5 * rateLimitMultiplier,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("admin-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5 * rateLimitMultiplier,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200 * rateLimitMultiplier,
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

// HSTS (Faz 6.25): preload + includeSubDomains + 1 yıl max-age — hstspreload.org minimum.
// UseHsts() yalnızca non-Development'ta aktif (aşağıda). hstspreload.org submit'i
// production deploy sonrası manuel adım (DEFERRED).
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

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

// Faz 7.2b-2: Newsletter dispatcher — Task.Run + IServiceScopeFactory ile arka plan job processing.
// Singleton — her Dispatch çağrısı kendi scope'unu yaratıp NewsletterService resolve eder.
builder.Services.AddSingleton<
    KucukMericHukuk.Core.Interfaces.Services.INewsletterDispatcher,
    KucukMericHukuk.Web.Infrastructure.NewsletterDispatcher>();

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

// AntiForgery: JSON body POST'lar için header destekle (Faz 6.6b — Faq drag-drop reorder).
// Form POST'ları etkilemez (default __RequestVerificationToken body field çalışmaya devam eder).
builder.Services.AddAntiforgery(opts =>
{
    opts.HeaderName = "RequestVerificationToken";
});

// Response Compression (Faz 6.17): CDN'den jsdelivr brotli serve ediyordu; self-host sonrası
// dev/test ortamında Bootstrap+Lucide uncompressed → mobile total size 397→1201 KiB. Brotli+Gzip
// ile bunu telafi ediyoruz. EnableForHttps=true: BREACH attack riski yok (response body'de
// secret + user-controlled content yok; static asset + Razor HTML compress safe).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/octet-stream",
        "image/svg+xml",
        "application/manifest+json"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

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

// Response Compression (Faz 6.17) — UseStaticFiles'tan ÖNCE olmalı (sıra önemli).
app.UseResponseCompression();

// Static asset cache — asp-append-version="true" + hashed querystring zaten cache-busting
// yapıyor (file değişince ?v=hash değişir, browser cache miss → yeni dosya). Bu nedenle
// 1 yıl immutable safe. Lighthouse "Use efficient cache lifetimes" puanını 50→100 çıkarır.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] =
            "public, max-age=31536000, immutable";
    }
});

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

// Production env'da admin seed credentials + Turnstile key'leri zorunlu — eksikse startup fail.
// Dev'de DbInitializer warn ile devam ediyor + Turnstile demo key'lere düşüyor; production'da bunlar kabul edilemez.
if (app.Environment.IsProduction())
{
    var seedOptions = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>()
        ?? new SeedOptions();
    seedOptions.ValidateForProduction();

    // Faz 6.25: Turnstile production enforcement — demo key fallback'i YASAK.
    var turnstileOptions = app.Configuration.GetSection(TurnstileOptions.SectionName).Get<TurnstileOptions>()
        ?? new TurnstileOptions();
    turnstileOptions.ValidateForProduction();
}

// DbInitializer (Testing ortamında çalıştırma — testler izole DB kullanıyor)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await initializer.InitializeAsync();
}

app.Run();

public partial class Program { }

using System.Globalization;
using KucukMericHukuk.Business;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.DataAccess;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.Infrastructure;
using KucukMericHukuk.Infrastructure.Initialization;
using KucukMericHukuk.Web.Areas.Admin.Identity;
using KucukMericHukuk.Web.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.Razor;
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

// Repository + UnitOfWork
builder.Services.AddDataAccess();

// Infrastructure (SeedOptions + DbInitializer)
builder.Services.AddInfrastructure(builder.Configuration);

// Mapster + Business services
builder.Services.AddBusiness();

// Web katmanindaki Mapster IRegister'lari (PageFormMappingConfig vb.) GlobalSettings'e ekle
Mapster.TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

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
builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Localization middleware: routing'ten SONRA, auth'tan ÖNCE
var localizationOptions = app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

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

using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Infrastructure;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Test!Pass123.";
    public const string AdminRoleName = "Admin";

    // Editor rolü: tüm makaleleri görür ve düzenler (review/edit görevi).
    public const string EditorEmail = "editor@test.local";
    public const string EditorPassword = "Test!Pass123.";
    public const string EditorRoleName = "Editor";

    // Author rolü: sadece kendi makalelerini görür ve düzenler.
    public const string AuthorEmail = "author@test.local";
    public const string AuthorPassword = "Test!Pass123.";
    public const string AuthorRoleName = "Author";

    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs Testing environment'ında AppDbContext registration'ını ve
        // DbInitializer çağrısını atlar — burada SQLite in-memory ile yeniden kayıt.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            // Production'da Cookie.SecurePolicy=Always (HTTPS-only). WebApplicationFactory
            // HTTP üzerinden test eder → cookie subsequent request'lerde gönderilmez,
            // auth ayakta tutulamaz. Test ortamında SameAsRequest'e indirgenir.
            services.PostConfigure<CookieAuthenticationOptions>(
                IdentityConstants.ApplicationScheme,
                options =>
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ctx.Database.EnsureCreated();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            SeedAsync(userManager, roleManager).GetAwaiter().GetResult();
        });
    }

    private static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Production seed ile uyumlu: 3 sabit rol (Faz 6.8)
        foreach (var roleName in new[] { AdminRoleName, "Editor", "Author" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var user = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FullName = "Test Admin"
            };

            var result = await userManager.CreateAsync(user, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, AdminRoleName);
            }
        }

        if (await userManager.FindByEmailAsync(EditorEmail) is null)
        {
            var user = new ApplicationUser
            {
                UserName = EditorEmail,
                Email = EditorEmail,
                EmailConfirmed = true,
                FullName = "Test Editor"
            };

            var result = await userManager.CreateAsync(user, EditorPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, EditorRoleName);
            }
        }

        if (await userManager.FindByEmailAsync(AuthorEmail) is null)
        {
            var user = new ApplicationUser
            {
                UserName = AuthorEmail,
                Email = AuthorEmail,
                EmailConfirmed = true,
                FullName = "Test Author"
            };

            var result = await userManager.CreateAsync(user, AuthorPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, AuthorRoleName);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}

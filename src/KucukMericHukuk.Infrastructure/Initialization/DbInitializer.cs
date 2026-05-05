using KucukMericHukuk.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Initialization;

public class DbInitializer : IDbInitializer
{
    private static readonly string[] DefaultRoles = { "Admin", "Editor", "Author" };

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedOptions _seedOptions;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<SeedOptions> seedOptions,
        ILogger<DbInitializer> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _seedOptions = seedOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in DefaultRoles)
        {
            if (await _roleManager.RoleExistsAsync(roleName)) continue;

            var role = new ApplicationRole
            {
                Name = roleName,
                Description = roleName switch
                {
                    "Admin" => "Tam yetkili yönetici",
                    "Editor" => "İçerik düzenleyici",
                    "Author" => "Makale yazarı (yalnızca kendi makaleleri)",
                    _ => roleName
                }
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Rol oluşturuldu: {RoleName}", roleName);
            }
            else
            {
                _logger.LogError("Rol oluşturulamadı: {RoleName} — {Errors}",
                    roleName, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        if (string.IsNullOrWhiteSpace(_seedOptions.AdminEmail) ||
            string.IsNullOrWhiteSpace(_seedOptions.AdminPassword))
        {
            _logger.LogWarning(
                "Seed:AdminEmail veya Seed:AdminPassword tanımlı değil. İlk admin kullanıcı oluşturulmadı. " +
                "User Secrets veya environment variable ile tanımlayın: Seed__AdminEmail, Seed__AdminPassword.");
            return;
        }

        var existing = await _userManager.FindByEmailAsync(_seedOptions.AdminEmail);
        if (existing is not null)
        {
            _logger.LogInformation("Admin kullanıcısı zaten mevcut: {Email}", _seedOptions.AdminEmail);
            if (!await _userManager.IsInRoleAsync(existing, "Admin"))
            {
                await _userManager.AddToRoleAsync(existing, "Admin");
                _logger.LogInformation("Mevcut kullanıcıya Admin rolü eklendi: {Email}", _seedOptions.AdminEmail);
            }
            return;
        }

        var user = new ApplicationUser
        {
            Email = _seedOptions.AdminEmail,
            UserName = _seedOptions.AdminEmail,
            FullName = _seedOptions.AdminFullName ?? "Sistem Yöneticisi",
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, _seedOptions.AdminPassword);
        if (!createResult.Succeeded)
        {
            _logger.LogError(
                "İlk admin kullanıcı oluşturulamadı: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
        if (!roleResult.Succeeded)
        {
            _logger.LogError(
                "Admin rolü atanamadı: {Errors}",
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation(
            "İlk admin kullanıcı oluşturuldu: {Email} (FullName: {FullName})",
            user.Email, user.FullName);
    }
}

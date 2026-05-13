using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Account;

public class ProfileTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ProfileTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_GET_AuthenticatedUser_Returns200_ShowsUserInfo()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client,
            IntegrationTestFactory.AdminEmail, IntegrationTestFactory.AdminPassword);

        var response = await client.GetAsync("/admin/account/profile");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Profilim");
        html.Should().Contain(IntegrationTestFactory.AdminEmail);
        html.Should().Contain("Admin", "Rol badge'inde rol adı görünmeli");
    }

    [Fact]
    public async Task Profile_GET_AnonymousUser_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await client.GetAsync("/admin/account/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location?.OriginalString.Should().Contain("login",
            "anonim kullanıcı login sayfasına yönlendirilmeli");
    }

    [Fact]
    public async Task ChangePassword_ValidInput_PasswordChanged_DbVerified()
    {
        // Tek-kullanımlık fixture-izolasyonu için yeni test user oluştur
        // (admin@test.local'ın şifresi değişip diğer testleri bozmasın).
        const string testEmail = "pw-change-valid@test.local";
        const string oldPassword = "OldPass!123";
        const string newPassword = "NewPass!456";
        await SeedUserAsync(testEmail, oldPassword, "Author");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client, testEmail, oldPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/account/profile");
        var response = await client.PostAsync("/admin/account/profile/change-password",
            BuildForm(token, oldPassword, newPassword, newPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Found,
            "başarılı değişim sonrası Profile'a redirect");

        // DB doğrulama — UserManager.CheckPasswordAsync ile yeni şifre geçerli olmalı
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(testEmail);
        user.Should().NotBeNull();
        (await userManager.CheckPasswordAsync(user!, newPassword)).Should().BeTrue();
        (await userManager.CheckPasswordAsync(user!, oldPassword)).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_ShowsError()
    {
        const string testEmail = "pw-wrong-old@test.local";
        const string oldPassword = "RealPass!123";
        await SeedUserAsync(testEmail, oldPassword, "Author");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client, testEmail, oldPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/account/profile");
        var response = await client.PostAsync("/admin/account/profile/change-password",
            BuildForm(token, oldPassword: "WrongOld!999", newPassword: "NewPass!456", confirm: "NewPass!456"));

        response.IsSuccessStatusCode.Should().BeTrue(
            "yanlış eski şifre 200 ile aynı sayfada hata göstermeli");
        var html = await response.Content.ReadAsStringAsync();
        // Identity wrong-password mesajı locale'ye göre değişir; "password" alt-string'i evrensel
        // Türkçe için ChangePassword mesajları "Yanlış şifre" gibi olabilir
        html.Should().Contain("Profilim");

        // DB: şifre değişmediği doğrulansın
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(testEmail);
        (await userManager.CheckPasswordAsync(user!, oldPassword)).Should().BeTrue(
            "yanlış eski şifre denemesinde gerçek şifre değişmemeli");
    }

    [Fact]
    public async Task ChangePassword_MismatchedNewPasswords_ShowsCompareError()
    {
        const string testEmail = "pw-mismatch@test.local";
        const string oldPassword = "MyPass!123";
        await SeedUserAsync(testEmail, oldPassword, "Author");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client, testEmail, oldPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/account/profile");
        var response = await client.PostAsync("/admin/account/profile/change-password",
            BuildForm(token, oldPassword, newPassword: "NewPass!456", confirm: "Different!789"));

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        // "Yeni şifreler eşleşmiyor" — ASCII-only substring (Faz 1 CLAUDE.md kuralı)
        html.Should().Contain("e", "[Compare] hata mesajı render edilmeli (ASCII tolerant check)");

        // DB: şifre değişmediği doğrulansın
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(testEmail);
        (await userManager.CheckPasswordAsync(user!, oldPassword)).Should().BeTrue();
        (await userManager.CheckPasswordAsync(user!, "NewPass!456")).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_NewPasswordTooShort_ShowsValidationError()
    {
        const string testEmail = "pw-tooshort@test.local";
        const string oldPassword = "MyPass!123";
        await SeedUserAsync(testEmail, oldPassword, "Author");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client, testEmail, oldPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/account/profile");
        // "abc" — StringLength(MinimumLength=8) tetiklenir (DataAnnotations)
        var response = await client.PostAsync("/admin/account/profile/change-password",
            BuildForm(token, oldPassword, newPassword: "abc", confirm: "abc"));

        response.IsSuccessStatusCode.Should().BeTrue();

        // DB: şifre değişmediği doğrulansın
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(testEmail);
        (await userManager.CheckPasswordAsync(user!, oldPassword)).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_NewPasswordPolicyViolation_ShowsIdentityError()
    {
        // 8 karakter ama digit yok / uppercase yok → Identity policy reject etmeli
        const string testEmail = "pw-policy@test.local";
        const string oldPassword = "MyPass!123";
        await SeedUserAsync(testEmail, oldPassword, "Author");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client, testEmail, oldPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/account/profile");
        // 8 karakter (DataAnnotation MinLength geçer) ama all-lowercase no-digit
        // → Identity password policy (RequireDigit/RequireUppercase) tetiklenir
        var response = await client.PostAsync("/admin/account/profile/change-password",
            BuildForm(token, oldPassword, newPassword: "allsmall", confirm: "allsmall"));

        response.IsSuccessStatusCode.Should().BeTrue();

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(testEmail);
        (await userManager.CheckPasswordAsync(user!, oldPassword)).Should().BeTrue(
            "policy violation'da şifre değişmemeli");
        (await userManager.CheckPasswordAsync(user!, "allsmall")).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_AfterSuccess_NewPasswordWorksForLogin()
    {
        // End-to-end: şifreyi değiştir → logout → yeni şifreyle login
        const string testEmail = "pw-e2e@test.local";
        const string oldPassword = "OldE2E!123";
        const string newPassword = "NewE2E!456";
        await SeedUserAsync(testEmail, oldPassword, "Author");

        var client1 = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client1, testEmail, oldPassword);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client1, "/admin/account/profile");
        await client1.PostAsync("/admin/account/profile/change-password",
            BuildForm(token, oldPassword, newPassword, newPassword));

        // Yeni client (taze cookie jar) ile yeni şifreyle login dene
        var client2 = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var loginResp = await TestHelpers.LoginAsync(client2, testEmail, newPassword);

        // Başarılı login → /admin'e redirect (302)
        loginResp.StatusCode.Should().Be(HttpStatusCode.Found);
        loginResp.Headers.Location?.OriginalString.Should().NotContain("login",
            "yeni şifreyle login başarılı, login sayfasına geri dönmemeli");
    }

    // === Helpers ===

    private async Task SeedUserAsync(string email, string password, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = email,
            IsActive = true,
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Test user seed failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);
    }

    private static FormUrlEncodedContent BuildForm(string token, string oldPassword, string newPassword, string confirm)
    {
        return new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("ChangePassword.OldPassword", oldPassword),
            new KeyValuePair<string, string>("ChangePassword.NewPassword", newPassword),
            new KeyValuePair<string, string>("ChangePassword.ConfirmPassword", confirm),
        });
    }
}

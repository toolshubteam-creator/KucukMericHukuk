using FluentAssertions;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.User;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// UserManagementService — real UserManager + in-memory SQLite Identity store ile test.
/// Identity setup karmaşık olduğundan ortak helper yerine her test kendi ServiceProvider'ını kurar.
/// </summary>
public class UserManagementServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;

    public UserManagementServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddLogging();
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Roller seed (3 sabit)
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var roleName in new[] { "Admin", "Editor", "Author" })
        {
            roleManager.CreateAsync(new ApplicationRole { Name = roleName }).GetAwaiter().GetResult();
        }
    }

    private UserManagementService CreateSut(out IServiceScope scope, out UserManager<ApplicationUser> userManager)
    {
        scope = _serviceProvider.CreateScope();
        userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        return new UserManagementService(
            userManager,
            roleManager,
            new UserCreateInputValidator(),
            new UserUpdateInputValidator(),
            new ResetPasswordInputValidator(),
            NullLogger<UserManagementService>.Instance);
    }

    private async Task<int> CreateUserAsync(UserManager<ApplicationUser> userManager,
        string userName, string email, string password, string role)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
        };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
        return user.Id;
    }

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesUserAndAssignsRole()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var input = new UserCreateInputDto
        {
            UserName = "newuser",
            Email = "new@test.com",
            FullName = "New User",
            Password = "Pass1234!",
            Role = "Editor",
        };

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();
        var created = await userManager.FindByEmailAsync("new@test.com");
        created.Should().NotBeNull();
        var roles = await userManager.GetRolesAsync(created!);
        roles.Should().Contain("Editor");
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ReturnsFailure()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        await CreateUserAsync(userManager, "first", "dup@test.com", "Pass1234!", "Admin");

        var result = await sut.CreateAsync(new UserCreateInputDto
        {
            UserName = "second",
            Email = "dup@test.com",
            Password = "Pass1234!",
            Role = "Editor",
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.User.EmailAlreadyExists);
    }

    [Fact]
    public async Task CreateAsync_InvalidRole_ReturnsFailure()
    {
        var sut = CreateSut(out var scope, out var _);
        using var _scope = scope;

        var result = await sut.CreateAsync(new UserCreateInputDto
        {
            UserName = "u", Email = "u@test.com", Password = "Pass1234!", Role = "SuperAdmin",
        });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.User.InvalidRole);
    }

    [Fact]
    public async Task UpdateAsync_SelfRoleChange_ReturnsCannotChangeOwnRole()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var id = await CreateUserAsync(userManager, "selfadmin", "self@test.com", "Pass1234!", "Admin");

        var result = await sut.UpdateAsync(new UserUpdateInputDto
        {
            Id = id,
            UserName = "selfadmin",
            Email = "self@test.com",
            Role = "Editor", // değişti
        }, currentUserId: id);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.User.CannotChangeOwnRole);
    }

    [Fact]
    public async Task UpdateAsync_SameRole_AllowsSelfUpdate()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var id = await CreateUserAsync(userManager, "selfok", "selfok@test.com", "Pass1234!", "Admin");

        var result = await sut.UpdateAsync(new UserUpdateInputDto
        {
            Id = id,
            UserName = "selfok",
            Email = "selfok@test.com",
            FullName = "New Name",
            Role = "Admin", // aynı rol
        }, currentUserId: id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidNewPassword_Succeeds()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var id = await CreateUserAsync(userManager, "pw", "pw@test.com", "Pass1234!", "Editor");

        var result = await sut.ResetPasswordAsync(new ResetPasswordInputDto
        {
            UserId = id,
            NewPassword = "NewPass1234!",
        });

        result.IsSuccess.Should().BeTrue();

        var user = await userManager.FindByIdAsync(id.ToString());
        var valid = await userManager.CheckPasswordAsync(user!, "NewPass1234!");
        valid.Should().BeTrue();
    }

    [Fact]
    public async Task UnlockAsync_LockedUser_Unlocks()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var id = await CreateUserAsync(userManager, "lock", "lock@test.com", "Pass1234!", "Editor");
        var user = await userManager.FindByIdAsync(id.ToString());

        // Manuel lockout
        await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddDays(1));
        await userManager.AccessFailedAsync(user!);
        var lockedBefore = await userManager.IsLockedOutAsync(user!);
        lockedBefore.Should().BeTrue();

        var result = await sut.UnlockAsync(id);

        result.IsSuccess.Should().BeTrue();
        var lockedAfter = await userManager.IsLockedOutAsync(user!);
        lockedAfter.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_SelfDeactivate_ReturnsFailure()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var id = await CreateUserAsync(userManager, "self", "self@test.com", "Pass1234!", "Admin");

        var result = await sut.DeactivateAsync(id, currentUserId: id);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.User.CannotDeactivateSelf);
    }

    [Fact]
    public async Task DeactivateAsync_LastActiveAdmin_ReturnsFailure()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var adminId = await CreateUserAsync(userManager, "lastadmin", "last@test.com", "Pass1234!", "Admin");
        // Admin'in deactivasyonunu başka bir admin yapmaya çalışıyor
        var otherUserId = await CreateUserAsync(userManager, "other", "other@test.com", "Pass1234!", "Editor");

        var result = await sut.DeactivateAsync(adminId, currentUserId: otherUserId);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.User.CannotDeactivateLastAdmin);
    }

    [Fact]
    public async Task DeactivateAsync_NonAdmin_SetsInactive()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var adminId = await CreateUserAsync(userManager, "admin", "admin@test.com", "Pass1234!", "Admin");
        var editorId = await CreateUserAsync(userManager, "editor", "editor@test.com", "Pass1234!", "Editor");

        var result = await sut.DeactivateAsync(editorId, currentUserId: adminId);

        result.IsSuccess.Should().BeTrue();
        var user = await userManager.FindByIdAsync(editorId.ToString());
        user!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAsync_DeactivatedUser_SetsActive()
    {
        var sut = CreateSut(out var scope, out var userManager);
        using var _ = scope;

        var adminId = await CreateUserAsync(userManager, "admin", "admin@test.com", "Pass1234!", "Admin");
        var editorId = await CreateUserAsync(userManager, "editor", "editor@test.com", "Pass1234!", "Editor");

        await sut.DeactivateAsync(editorId, currentUserId: adminId);

        var result = await sut.RestoreAsync(editorId);

        result.IsSuccess.Should().BeTrue();
        var user = await userManager.FindByIdAsync(editorId.ToString());
        user!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        var sut = CreateSut(out var scope, out var _);
        using var _scope = scope;

        var result = await sut.GetByIdAsync(99999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.User.NotFound);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
    }
}

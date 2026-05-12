using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.User;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Business.Services;

public class UserManagementService : IUserManagementService
{
    private static readonly string[] AvailableRoles = { "Admin", "Editor", "Author" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IValidator<UserCreateInputDto> _createValidator;
    private readonly IValidator<UserUpdateInputDto> _updateValidator;
    private readonly IValidator<ResetPasswordInputDto> _resetPasswordValidator;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IValidator<UserCreateInputDto> createValidator,
        IValidator<UserUpdateInputDto> updateValidator,
        IValidator<ResetPasswordInputDto> resetPasswordValidator,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _logger = logger;
    }

    public Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(AvailableRoles);

    public async Task<Result<PagedResult<UserListDto>>> GetPagedAsync(UserQueryDto query, CancellationToken ct = default)
    {
        if (query.Page < 1) query.Page = 1;
        if (query.PageSize < 1) query.PageSize = 20;
        if (query.PageSize > 100) query.PageSize = 100;

        // Filter: keyword (username/email/fullname), IsActive
        var users = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            users = users.Where(u =>
                u.UserName!.Contains(k) ||
                u.Email!.Contains(k) ||
                (u.FullName != null && u.FullName.Contains(k)));
        }

        if (query.IsActive.HasValue)
        {
            var active = query.IsActive.Value;
            users = users.Where(u => u.IsActive == active);
        }

        users = users.OrderBy(u => u.UserName);

        var totalCount = await users.CountAsync(ct);
        var page = await users
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        // Role filter post-fetch (UserManager.GetRolesAsync per-user — small set, OK)
        var items = new List<UserListDto>();
        foreach (var u in page)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var role = roles.FirstOrDefault();

            if (!string.IsNullOrEmpty(query.Role) && !string.Equals(role, query.Role, StringComparison.OrdinalIgnoreCase))
                continue;

            items.Add(new UserListDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                Role = role,
                IsActive = u.IsActive,
                IsLockedOut = await _userManager.IsLockedOutAsync(u),
                CreatedAt = u.CreatedAt,
            });
        }

        var paged = new PagedResult<UserListDto>(items, totalCount, query.Page, query.PageSize);
        return Result.Success(paged);
    }

    public async Task<Result<UserDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.Failure<UserDetailDto>(new Error(ErrorCodes.User.NotFound, "Kullanıcı bulunamadı."));

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault(),
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            IsLockedOut = await _userManager.IsLockedOutAsync(user),
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        });
    }

    public async Task<Result<int>> CreateAsync(UserCreateInputDto input, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        if (!AvailableRoles.Contains(input.Role))
            return Result.Failure<int>(new Error(ErrorCodes.User.InvalidRole, "Geçersiz rol seçimi."));

        var existingByEmail = await _userManager.FindByEmailAsync(input.Email);
        if (existingByEmail is not null)
            return Result.Failure<int>(new Error(ErrorCodes.User.EmailAlreadyExists, "Bu e-posta zaten kayıtlı."));

        var existingByUserName = await _userManager.FindByNameAsync(input.UserName);
        if (existingByUserName is not null)
            return Result.Failure<int>(new Error(ErrorCodes.User.UserNameAlreadyExists, "Bu kullanıcı adı zaten kayıtlı."));

        var user = new ApplicationUser
        {
            UserName = input.UserName.Trim(),
            Email = input.Email.Trim().ToLowerInvariant(),
            FullName = string.IsNullOrWhiteSpace(input.FullName) ? null : input.FullName.Trim(),
            EmailConfirmed = true,
            IsActive = true,
        };

        var createResult = await _userManager.CreateAsync(user, input.Password);
        if (!createResult.Succeeded)
            return Result.Failure<int>(MapIdentityErrors(createResult));

        var roleResult = await _userManager.AddToRoleAsync(user, input.Role);
        if (!roleResult.Succeeded)
        {
            // Rollback: kullanıcı oluşturuldu ama rol atanmadı — kullanıcıyı sil
            await _userManager.DeleteAsync(user);
            return Result.Failure<int>(MapIdentityErrors(roleResult));
        }

        _logger.LogInformation("Kullanıcı oluşturuldu: {UserName} (Id={Id}), rol={Role}", user.UserName, user.Id, input.Role);
        return Result.Success(user.Id);
    }

    public async Task<Result> UpdateAsync(UserUpdateInputDto input, int currentUserId, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult();

        if (!AvailableRoles.Contains(input.Role))
            return Result.Failure(new Error(ErrorCodes.User.InvalidRole, "Geçersiz rol seçimi."));

        var user = await _userManager.FindByIdAsync(input.Id.ToString());
        if (user is null)
            return Result.Failure(new Error(ErrorCodes.User.NotFound, "Kullanıcı bulunamadı."));

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();

        // Guard: kendi rolünü değiştiremez
        if (user.Id == currentUserId && !string.Equals(currentRole, input.Role, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new Error(
                ErrorCodes.User.CannotChangeOwnRole,
                "Kendi rolünüzü değiştiremezsiniz. Başka bir Admin'in yapması gerekir."));
        }

        // Email değişiyorsa unique kontrolü
        var newEmail = input.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var other = await _userManager.FindByEmailAsync(newEmail);
            if (other is not null && other.Id != user.Id)
                return Result.Failure(new Error(ErrorCodes.User.EmailAlreadyExists, "Bu e-posta başka bir kullanıcıda kayıtlı."));
        }

        // UserName değişiyorsa unique kontrolü
        var newUserName = input.UserName.Trim();
        if (!string.Equals(user.UserName, newUserName, StringComparison.OrdinalIgnoreCase))
        {
            var other = await _userManager.FindByNameAsync(newUserName);
            if (other is not null && other.Id != user.Id)
                return Result.Failure(new Error(ErrorCodes.User.UserNameAlreadyExists, "Bu kullanıcı adı başka birine ait."));
        }

        user.UserName = newUserName;
        user.Email = newEmail;
        user.NormalizedUserName = newUserName.ToUpperInvariant();
        user.NormalizedEmail = newEmail.ToUpperInvariant();
        user.FullName = string.IsNullOrWhiteSpace(input.FullName) ? null : input.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Failure(MapIdentityErrors(updateResult));

        // Rol değişti mi
        if (!string.Equals(currentRole, input.Role, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(currentRole))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, currentRole);
                if (!removeResult.Succeeded)
                    return Result.Failure(MapIdentityErrors(removeResult));
            }

            var addResult = await _userManager.AddToRoleAsync(user, input.Role);
            if (!addResult.Succeeded)
                return Result.Failure(MapIdentityErrors(addResult));
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordInputDto input, CancellationToken ct = default)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult();

        var user = await _userManager.FindByIdAsync(input.UserId.ToString());
        if (user is null)
            return Result.Failure(new Error(ErrorCodes.User.NotFound, "Kullanıcı bulunamadı."));

        // Admin manuel password reset: token üret + reset (token flow OFF — kullanıcı email almıyor)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, input.NewPassword);

        if (!result.Succeeded)
            return Result.Failure(MapIdentityErrors(result));

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Kullanıcı şifresi sıfırlandı: Id={Id}", user.Id);
        return Result.Success();
    }

    public async Task<Result> UnlockAsync(int userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(new Error(ErrorCodes.User.NotFound, "Kullanıcı bulunamadı."));

        var lockoutEndResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!lockoutEndResult.Succeeded)
            return Result.Failure(MapIdentityErrors(lockoutEndResult));

        var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
        if (!resetResult.Succeeded)
            return Result.Failure(MapIdentityErrors(resetResult));

        _logger.LogInformation("Kullanıcı kilidi açıldı: Id={Id}", user.Id);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int userId, int currentUserId, CancellationToken ct = default)
    {
        if (userId == currentUserId)
        {
            return Result.Failure(new Error(
                ErrorCodes.User.CannotDeactivateSelf,
                "Kendinizi devre dışı bırakamazsınız."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(new Error(ErrorCodes.User.NotFound, "Kullanıcı bulunamadı."));

        // Son admin guard
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Contains("Admin"))
        {
            var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            var activeAdminCount = allAdmins.Count(a => a.IsActive);
            if (activeAdminCount <= 1)
            {
                return Result.Failure(new Error(
                    ErrorCodes.User.CannotDeactivateLastAdmin,
                    "Son aktif Admin kullanıcı devre dışı bırakılamaz. Önce başka bir Admin oluşturun."));
            }
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Failure(MapIdentityErrors(result));

        _logger.LogInformation("Kullanıcı devre dışı bırakıldı: Id={Id}", user.Id);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(new Error(ErrorCodes.User.NotFound, "Kullanıcı bulunamadı."));

        if (user.IsActive) return Result.Success();

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Failure(MapIdentityErrors(result));

        _logger.LogInformation("Kullanıcı yeniden etkinleştirildi: Id={Id}", user.Id);
        return Result.Success();
    }

    private static IReadOnlyList<Error> MapIdentityErrors(IdentityResult identityResult)
    {
        return identityResult.Errors
            .Select(e => new Error(ErrorCodes.User.IdentityError, e.Description))
            .ToArray();
    }
}

using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.User;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IUserManagementService
{
    Task<Result<PagedResult<UserListDto>>> GetPagedAsync(UserQueryDto query, CancellationToken ct = default);

    Task<Result<UserDetailDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Yeni kullanıcı oluşturur ve seçilen role atar. Email uniqueness kontrolü Identity tarafından yapılır.</summary>
    Task<Result<int>> CreateAsync(UserCreateInputDto input, CancellationToken ct = default);

    /// <summary>
    /// Kullanıcı bilgilerini günceller. Self-role-change blokesi var (currentUserId == input.Id ise rol değişikliği yasak).
    /// </summary>
    Task<Result> UpdateAsync(UserUpdateInputDto input, int currentUserId, CancellationToken ct = default);

    /// <summary>Admin manuel password reset. Token flow YOK — KISS.</summary>
    Task<Result> ResetPasswordAsync(ResetPasswordInputDto input, CancellationToken ct = default);

    /// <summary>Lockout reset: LockoutEnd=null + AccessFailedCount=0.</summary>
    Task<Result> UnlockAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Kullanıcı devre dışı (IsActive=false). Guards: self-deactivate yasak, son admin'i deactive yasak.
    /// </summary>
    Task<Result> DeactivateAsync(int userId, int currentUserId, CancellationToken ct = default);

    /// <summary>IsActive=true (re-enable login).</summary>
    Task<Result> RestoreAsync(int userId, CancellationToken ct = default);

    /// <summary>Mevcut sabit roller (Admin/Editor/Author) — dropdown'larda kullanılır.</summary>
    Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken ct = default);
}

using System.ComponentModel.DataAnnotations;
using KucukMericHukuk.Core.DTOs.User;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Users;

public class UserListViewModel
{
    public UserQueryDto Query { get; set; } = new();
    public IReadOnlyList<UserListDto> Items { get; set; } = Array.Empty<UserListDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}

public class UserCreateFormViewModel
{
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Ad Soyad")]
    public string? FullName { get; set; }

    [Display(Name = "Şifre")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Şifre (Tekrar)")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}

public class UserEditFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Ad Soyad")]
    public string? FullName { get; set; }

    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsSelf { get; set; }

    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}

public class ResetPasswordFormViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Yeni Şifre")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni Şifre (Tekrar)")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

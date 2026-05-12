using System.Security.Claims;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.User;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Users;
using KucukMericHukuk.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/users")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IUserManagementService _service;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserManagementService service, ILogger<UsersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int CurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(UserQueryDto query, CancellationToken ct)
    {
        ViewData["Title"] = "Kullanıcılar";

        var result = await _service.GetPagedAsync(query, ct);
        var roles = await _service.GetAvailableRolesAsync(ct);

        if (result.IsFailure)
        {
            TempData["Error"] = "Kullanıcı listesi yüklenirken bir sorun oluştu.";
            return View(new UserListViewModel { Query = query, AvailableRoles = roles });
        }

        var vm = new UserListViewModel
        {
            Query = query,
            Items = result.Value.Items,
            TotalCount = result.Value.TotalCount,
            PageNumber = result.Value.PageNumber,
            PageSize = result.Value.PageSize,
            TotalPages = result.Value.TotalPages,
            HasPrevious = result.Value.HasPrevious,
            HasNext = result.Value.HasNext,
            AvailableRoles = roles,
        };

        ViewBag.CurrentUserId = CurrentUserId();
        return View(vm);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Kullanıcı Detayı";

        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.User.NotFound)
                return NotFound();
            TempData["Error"] = "Kullanıcı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.CurrentUserId = CurrentUserId();
        return View(result.Value);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Kullanıcı";

        var roles = await _service.GetAvailableRolesAsync(ct);
        var vm = new UserCreateFormViewModel
        {
            Role = "Editor",
            AvailableRoles = roles,
        };
        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Yeni Kullanıcı";
        form.AvailableRoles = await _service.GetAvailableRolesAsync(ct);

        var input = new UserCreateInputDto
        {
            UserName = form.UserName,
            Email = form.Email,
            FullName = form.FullName,
            Password = form.Password,
            Role = form.Role,
        };

        var result = await _service.CreateAsync(input, ct);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = $"Kullanıcı oluşturuldu: {form.UserName}";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Kullanıcı Düzenle";

        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.User.NotFound)
                return NotFound();
            TempData["Error"] = "Kullanıcı yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var u = result.Value;
        var vm = new UserEditFormViewModel
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            FullName = u.FullName,
            Role = u.Role ?? string.Empty,
            IsActive = u.IsActive,
            IsLockedOut = u.IsLockedOut,
            IsSelf = u.Id == CurrentUserId(),
            AvailableRoles = await _service.GetAvailableRolesAsync(ct),
        };

        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserEditFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Kullanıcı Düzenle";

        if (form.Id != id) return BadRequest();

        form.AvailableRoles = await _service.GetAvailableRolesAsync(ct);
        form.IsSelf = form.Id == CurrentUserId();

        var input = new UserUpdateInputDto
        {
            Id = form.Id,
            UserName = form.UserName,
            Email = form.Email,
            FullName = form.FullName,
            Role = form.Role,
        };

        var result = await _service.UpdateAsync(input, CurrentUserId(), ct);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "Kullanıcı güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("reset-password/{id:int}")]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Şifre Sıfırla";

        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.User.NotFound)
                return NotFound();
            return RedirectToAction(nameof(Index));
        }

        return View(new ResetPasswordFormViewModel
        {
            UserId = result.Value.Id,
            UserName = result.Value.UserName,
        });
    }

    [HttpPost("reset-password/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Şifre Sıfırla";

        if (form.UserId != id) return BadRequest();

        var input = new ResetPasswordInputDto
        {
            UserId = form.UserId,
            NewPassword = form.NewPassword,
        };

        var result = await _service.ResetPasswordAsync(input, ct);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            return View(form);
        }

        TempData["Success"] = "Şifre başarıyla sıfırlandı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("unlock/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id, CancellationToken ct)
    {
        var result = await _service.UnlockAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.User.NotFound)
                return NotFound();
            TempData["Error"] = result.FirstError?.Message ?? "Kilit açılırken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Hesap kilidi açıldı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _service.DeactivateAsync(id, CurrentUserId(), ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.User.NotFound)
                return NotFound();
            TempData["Error"] = result.FirstError?.Message ?? "Devre dışı bırakılırken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Kullanıcı devre dışı bırakıldı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _service.RestoreAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.User.NotFound)
                return NotFound();
            TempData["Error"] = result.FirstError?.Message ?? "Etkinleştirilirken bir sorun oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Kullanıcı yeniden etkinleştirildi.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

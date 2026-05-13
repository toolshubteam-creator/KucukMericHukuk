using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/account")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Admin");
        }

        ViewData["Title"] = "Giriş Yap";
        var vm = new LoginViewModel { ReturnUrl = returnUrl };
        return View(vm);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["Title"] = "Giriş Yap";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        // Faz 6.8: Devre dışı bırakılmış kullanıcı login yapamaz
        if (!user.IsActive)
        {
            _logger.LogWarning("Devre dışı kullanıcı login denemesi: {Email}", model.Email);
            ModelState.AddModelError(string.Empty,
                "Hesabınız devre dışı bırakılmıştır. Yönetici ile iletişime geçin.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Kullanıcı giriş yaptı: {Email}", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Kullanıcı kilitlendi: {Email}", model.Email);
            ModelState.AddModelError(string.Empty,
                "Hesabınız çok sayıda hatalı girişten dolayı geçici olarak kilitlendi. Lütfen 15 dakika sonra tekrar deneyin.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
        return View(model);
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Kullanıcı çıkış yaptı.");
        return RedirectToAction("Login");
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        ViewData["Title"] = "Profilim";

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var vm = await BuildProfileViewModelAsync(user);
        return View(vm);
    }

    [HttpPost("profile/change-password")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfileViewModel form)
    {
        ViewData["Title"] = "Profilim";

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        // DataAnnotations ChangePassword alt-objesi üzerinde — sadece o branch'i kontrol et.
        // (Üst-seviye Profile metin alanları read-only, formdan gelmez/gelmemeli.)
        if (!ModelState.IsValid)
        {
            var vm = await BuildProfileViewModelAsync(user);
            vm.ChangePassword = form.ChangePassword;
            return View(nameof(Profile), vm);
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            form.ChangePassword.OldPassword,
            form.ChangePassword.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                // Identity password policy hataları ChangePassword alanına bağlanır,
                // validation summary'de değil ilgili input grubunun yanında görünür.
                ModelState.AddModelError(
                    $"{nameof(ProfileViewModel.ChangePassword)}.{nameof(ChangePasswordInputModel.NewPassword)}",
                    err.Description);
            }

            var vm = await BuildProfileViewModelAsync(user);
            vm.ChangePassword = form.ChangePassword;
            return View(nameof(Profile), vm);
        }

        // Security stamp güncellendi — mevcut cookie geçerli ama refresh edilmeli ki
        // yeni hash ile claims yeniden üretilsin (diğer aktif oturumlar invalidate olur).
        await _signInManager.RefreshSignInAsync(user);

        _logger.LogInformation("Kullanıcı şifresi değiştirildi: {Email}", user.Email);
        TempData["Success"] = "Şifreniz başarıyla değiştirildi.";
        return RedirectToAction(nameof(Profile));
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new ProfileViewModel
        {
            FullName = user.FullName ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty,
        };
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Admin");
    }
}

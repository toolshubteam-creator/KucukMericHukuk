using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Admin");
    }
}

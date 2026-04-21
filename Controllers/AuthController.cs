using Microsoft.AspNetCore.Mvc;
using Airport.Services;
using Airport.ViewModels;
using Airport.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Airport.Data;
namespace Airport.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _authService.RegisterAsync(model);
                    await SignInAsync(user);
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _authService.LoginAsync(model);
                    await SignInAsync(user);
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            ViewBag.UpdateProfileModel  = new UpdateProfileViewModel  { Username = user.Username, Email = user.Email };
            ViewBag.ChangePasswordModel = new ChangePasswordViewModel();
            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userId0 = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var u0 = await _context.Users.FindAsync(userId0);
                ViewBag.UpdateProfileModel  = model;
                ViewBag.ChangePasswordModel = new ChangePasswordViewModel();
                ViewBag.ActiveTab = "profile";
                return View("Profile", u0);
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var updatedUser = await _authService.UpdateProfileAsync(userId, model.Username, model.Email);
                // Обновляем куки с новым именем/email
                await SignInAsync(updatedUser);
                TempData["ProfileSuccess"] = "Данные аккаунта успешно обновлены.";
            }
            catch (Exception ex)
            {
                TempData["ProfileError"] = ex.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userId0 = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var u0 = await _context.Users.FindAsync(userId0);
                ViewBag.UpdateProfileModel  = new UpdateProfileViewModel { Username = u0!.Username, Email = u0.Email };
                ViewBag.ChangePasswordModel = model;
                ViewBag.ActiveTab = "password";
                return View("Profile", u0);
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                await _authService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
                TempData["PasswordSuccess"] = "Пароль успешно изменён.";
            }
            catch (Exception ex)
            {
                TempData["PasswordError"] = ex.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        private async Task SignInAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,  user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role,  user.Role)
            };
            var identity   = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = true };
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties);
        }
    }
}

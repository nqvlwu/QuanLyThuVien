using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LibraryOS.Models;
using LibraryOS.Services;

namespace LibraryOS.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _auth;

        public AccountController(AuthService auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // ── Query Oracle ──────────────────────────────────────
            var user = _auth.Authenticate(model.Username, model.Password);

            if (user == null)
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName",      user.FullName),
                new Claim("Role",          user.Role),
                new Claim(ClaimTypes.Role, user.DbRole),
                new Claim("DbRole",        user.DbRole),
                new Claim("MaNV",          user.MaNV),     
                new Claim("SoTheTV",       user.SoTheTV),   
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToDashboard(user.Role);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private IActionResult RedirectToDashboard(string? role = null)
        {
            role ??= User.FindFirstValue("Role");
            return role switch
            {
                "ql" => RedirectToAction("Dashboard", "QuanLy"),
                "tt" => RedirectToAction("Dashboard", "ThuThu"),
                "dg" => RedirectToAction("Dashboard", "DocGia"),
                _ => RedirectToAction("Login")
            };
        }
    }
}
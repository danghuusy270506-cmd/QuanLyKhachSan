using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.test;
using System;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{

    [AllowAnonymous]
    public class DangNhapController : Controller
    {
        private readonly QLKhachSanContext _context;
        private readonly Authentication _jwtAuth;

        public DangNhapController(QLKhachSanContext context)
        {
            _context = context;
            _jwtAuth = new Authentication();
        }

       
        [HttpGet]
        public IActionResult Authentication()
        {
           
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Home");
                if (User.IsInRole("NhanVien")) return RedirectToAction("StaffIndex", "Home");
                return RedirectToAction("ClientIndex", "Home");
            }

            return View();
        }

        /
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authentication(string username, string password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!";
                return View();
            }
            
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.Username == username);

            // Không tìm thấy tài khoản
            if (user == null)
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
                return View();
            }

            // Kiểm tra định dạng mật khẩu đã lưu
            string storedPassword = user.Password ?? string.Empty;

            // Mật khẩu BCrypt thông thường bắt đầu bằng "$2"
            bool isBcryptHash = storedPassword.StartsWith(
                "$2", StringComparison.Ordinal);

            bool passwordValid = false;

            if (isBcryptHash)
            {
                
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(
                        password, storedPassword);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    
                    passwordValid = false;
                }
            }
            else
            {
                
                passwordValid = string.Equals(
                    password,
                    storedPassword,
                    StringComparison.Ordinal);
            }

            
            if (!passwordValid)
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
                return View();
            }

            
            if (user.TrangThai == "Locked" || user.TrangThai == "Khoa")
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa!";
                return View();
            }
            if (!isBcryptHash)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(
                    password, 10);

                await _context.SaveChangesAsync();
            }

            
            string token = _jwtAuth.GenerateToken(user);

            
            CookieOptions tokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,                  
                Secure = true,                    
                SameSite = SameSiteMode.Strict,   
                Expires = rememberMe ? DateTime.Now.AddDays(7) : DateTime.Now.AddMinutes(10)
            };
            Response.Cookies.Append("AccessToken", token, tokenCookieOptions);

            
            if (rememberMe)
            {
                CookieOptions savedUserOption = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true
                };
                Response.Cookies.Append("SavedUsername", user.Username ?? "", savedUserOption);
            }
            else
            {
                Response.Cookies.Delete("SavedUsername");
            }

            /
            TempData["SuccessMessage"] = "Đăng nhập thành công!";

            
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Home");
            }
            else if (user.Role == "NhanVien")
            {
                return RedirectToAction("StaffIndex", "Home");
            }
            else
            {
                return RedirectToAction("ClientIndex", "Home");
            }
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangXuat()
        {
            
            Response.Cookies.Delete(
                "AccessToken",
                new CookieOptions
                {
                    Path = "/",
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Strict
                });

            TempData["SuccessMessage"] = "Đăng xuất thành công!";

            
            return RedirectToAction("ClientIndex", "Home");
        }

    }
}
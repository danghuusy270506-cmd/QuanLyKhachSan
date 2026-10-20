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

        // 1. Hiển thị trang đăng nhập
        [HttpGet]
        public IActionResult Authentication()
        {
            // Nếu đã đăng nhập (Cookie AccessToken hợp lệ) -> Tự nhảy về trang theo Role
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Home");
                if (User.IsInRole("NhanVien")) return RedirectToAction("StaffIndex", "Home");
                return RedirectToAction("ClientIndex", "Home");
            }

            return View();
        }

        // 2. Xử lý đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authentication(string username, string password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!";
                return View();
            }
            // Tìm tài khoản theo Username
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
                // Xác minh mật khẩu BCrypt
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(
                        password, storedPassword);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Hash bị sai định dạng: không cho đăng nhập
                    passwordValid = false;
                }
            }
            else
            {
                // Tương thích tạm thời với tài khoản cũ
                // có mật khẩu lưu dạng văn bản thường
                passwordValid = string.Equals(
                    password,
                    storedPassword,
                    StringComparison.Ordinal);
            }

            // Mật khẩu không đúng hoặc hash không hợp lệ
            if (!passwordValid)
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
                return View();
            }

            // Kiểm tra trạng thái tài khoản
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

            // A. TẠO MÃ JWT TOKEN TỪ SERVICE AUTHENTICATION
            string token = _jwtAuth.GenerateToken(user);

            // B. LƯU TOKEN VÀO HTTPONLY COOKIE
            CookieOptions tokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,                  // Chống tấn công XSS
                Secure = true,                    // chỉ truyền qua https
                SameSite = SameSiteMode.Strict,   // chông csrs
                Expires = rememberMe ? DateTime.Now.AddDays(7) : DateTime.Now.AddHours(2)
            };
            Response.Cookies.Append("AccessToken", token, tokenCookieOptions);

            // C. GHI NHỚ USERNAME ĐỂ RE-FILL FORM ĐĂNG NHẬP (Nếu chọn Remember Me)
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

            // D. TẠO THÔNG BÁO FLASH MESSAGE
            TempData["SuccessMessage"] = "Đăng nhập thành công!";

            // E. ĐIỀU HƯỚNG TỚI TRANG HOME TƯƠNG ỨNG VỚI ROLE
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

        // 3. Đăng xuất
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangXuat()
        {
            // Xóa JWT cookie
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

            // Trở về website công khai sau khi đăng xuất
            return RedirectToAction("ClientIndex", "Home");
        }

    }
}
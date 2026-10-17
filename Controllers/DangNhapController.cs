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
    // BẮT BUỘC BỔ SUNG: Mở khóa hoàn toàn cho Controller này để không bị Middleware chặn
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
                if (User.IsInRole("Admin")) return RedirectToAction("AdminIndex", "Home");
                if (User.IsInRole("NhanVien")) return RedirectToAction("StaffIndex", "Home");
                return RedirectToAction("Index", "Home");
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

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
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

            // A. TẠO MÃ JWT TOKEN TỪ SERVICE AUTHENTICATION
            string token = _jwtAuth.GenerateToken(user);

            // B. LƯU TOKEN VÀO HTTPONLY COOKIE
            CookieOptions tokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,                  // Chống tấn công XSS
                Secure = true,                    // Chỉ truyền qua HTTPS
                SameSite = SameSiteMode.Strict,   // Chống CSRF
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
            TempData["SuccessMessage"] = $"Bạn đang đăng nhập với tư cách là {user.Role}";

            // E. ĐIỀU HƯỚNG TỚI TRANG HOME TƯƠNG ỨNG VỚI ROLE
            if (user.Role == "Admin")
            {
                return RedirectToAction("AdminIndex", "Home");
            }
            else if (user.Role == "NhanVien")
            {
                return RedirectToAction("StaffIndex", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // 3. Xử lý Đăng xuất (Chỉ cần xóa Cookie Token)
        [HttpGet]
        [HttpPost]
        public IActionResult DangXuat()
        {
            // Xóa Cookie AccessToken
            Response.Cookies.Delete("AccessToken");

            TempData["SuccessMessage"] = "Đăng xuất thành công!";

            // Điều hướng về lại trang Đăng nhập
            return RedirectToAction("Authentication");
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    public class DangNhapController : Controller
    {
        private readonly QLKhachSanContext _context;

        // Bỏ Inject Authentication service vì không dùng JWT Token nữa
        public DangNhapController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. Hiển thị trang đăng nhập
        [HttpGet]
        public IActionResult Authentication()
        {
            // Nếu đã đăng nhập (đã có Session) thì điều hướng ngay về Trang chủ
            if (HttpContext.Session.GetInt32("MaKhachHang") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // 2. Xử lý đăng nhập bằng Session & Cookies
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authentication(string username, string password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!";
                return View();
            }

            // Tìm khách hàng theo Username
            var user = await _context.KhachHangs
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
                return View();
            }

            // Kiểm tra mật khẩu băm BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (!isPasswordValid)
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
                return View();
            }

            // A. LƯU THÔNG TIN VÀO SESSION
            HttpContext.Session.SetInt32("MaKhachHang", user.MaKhachHang);
            HttpContext.Session.SetString("HoTen", user.HoTen ?? "");
            HttpContext.Session.SetString("Username", user.Username ?? "");

            // B. GHI NHỚ TÀI KHOẢN VÀO COOKIES (Nếu tích chọn Remember Me)
            if (rememberMe)
            {
                CookieOptions option = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(7), // Lưu cookie trong 7 ngày
                    HttpOnly = true,
                    IsEssential = true
                };
                Response.Cookies.Append("SavedUsername", user.Username, option);
            }
            else
            {
                Response.Cookies.Delete("SavedUsername");
            }

            // C. ĐIỀU HƯỚNG VỀ TRANG CHỦ HOẶC DASHBOARD DÀNH CHO KHÁCH HÀNG
            return RedirectToAction("Index", "Home");
        }

        // 3. Xử lý Đăng xuất (Xóa Session)
        [HttpGet]
        public IActionResult DangXuat()
        {
            // Xóa sạch dữ liệu Session
            HttpContext.Session.Clear();

            // Điều hướng về lại trang Đăng nhập
            return RedirectToAction("Authentication");
        }
    }
}
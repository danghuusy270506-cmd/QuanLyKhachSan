using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly QLKhachSanContext _context;

        public KhachHangController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. Danh sách khách hàng
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var khachHangs = await _context.KhachHangs.ToListAsync();
            return View(khachHangs);
        }

        // 2. Chi tiết 1 khách hàng
        [HttpGet]
        public async Task<IActionResult> Details(int maKhachHang)
        {
            if (maKhachHang <= 0)
            {
                return BadRequest("ID không hợp lệ!");
            }

            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);

            if (khachHang == null)
            {
                return NotFound("Không tìm thấy khách hàng có ID này!");
            }

            return View(khachHang);
        }

        // 3. Hiển thị trang Thêm mới
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 4. Xử lý Thêm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhachHang khachHang)
        {
            // 1. Kiểm tra các trường thông tin duy nhất (Unique)
            if (await _context.KhachHangs.AnyAsync(k => k.Email == khachHang.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
            }

            if (await _context.KhachHangs.AnyAsync(k => k.Cccd == khachHang.Cccd)) // Đổi Cccd thành tên thuộc tính thực tế của bạn
            {
                ModelState.AddModelError("Cccd", "Số CCCD này đã tồn tại trong hệ thống.");
            }

            if (await _context.KhachHangs.AnyAsync(k => k.Username == khachHang.Username)) // Đổi TenTaiKhoan thành tên thuộc tính thực tế
            {
                ModelState.AddModelError("Username", "Tên tài khoản này đã được sử dụng.");
            }

            // 2. Kiểm tra ModelState sau khi đã thêm các lỗi kiểm tra thủ công
            if (ModelState.IsValid)
            {
                // Băm mật khẩu trước khi lưu
                if (!string.IsNullOrEmpty(khachHang.Password))
                {
                    khachHang.Password = BCrypt.Net.BCrypt.HashPassword(khachHang.Password);
                }

                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { maKhachHang = khachHang.MaKhachHang });
            }

            return View(khachHang);
        }

        // 5. Hiển thị trang Cập nhật
        [HttpGet]
        public async Task<IActionResult> Edit(int maKhachHang)
        {
            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
            if (khachHang == null)
            {
                return NotFound("Không tìm thấy khách hàng có ID này!");
            }

            return View(khachHang);
        }

        // 6. Xử lý Cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int maKhachHang, KhachHang model, string? newPassword)
        {
            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
            if (khachHang == null)
            {
                return NotFound("Không tìm thấy khách hàng có ID này!");
            }

            // Cập nhật các thông tin cơ bản
            khachHang.HoTen = model.HoTen;
            khachHang.Cccd = model.Cccd;
            khachHang.Sdt = model.Sdt;
            khachHang.Email = model.Email;
            khachHang.Username = model.Username;

            // Chỉ cập nhật băm mật khẩu nếu người dùng nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                khachHang.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { maKhachHang = khachHang.MaKhachHang });
        }

        // 7. Xử lý Xóa khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int maKhachHang)
        {
            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
            if (khachHang == null)
            {
                return NotFound("Không tìm thấy khách hàng có ID này!");
            }

            _context.KhachHangs.Remove(khachHang);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
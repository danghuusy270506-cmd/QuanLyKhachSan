
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using System.Security.Claims;
using QuanLyKhachSan.Models.ViewModels;

namespace QuanLyKhachSan.Controllers
{
    [Authorize(Roles = "KhachHang")]
    public class ClientTaiKhoanController : Controller
    {
        private readonly QLKhachSanContext _context;

        public ClientTaiKhoanController(
            QLKhachSanContext context)
        {
            _context = context;
        }

        // Thông tin cá nhân của khách hàng đang đăng nhập
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            var khachHang = await _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    k => k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return NotFound();

            return View(khachHang);
        }

        // GET: Hiển thị form chỉnh sửa thông tin cá nhân
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Xác định tài khoản đang đăng nhập
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // Chỉ lấy hồ sơ khách hàng thuộc tài khoản hiện tại
            var khachHang = await _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    k => k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return NotFound();

            // Chuyển dữ liệu sang ViewModel
            var model = new ClientCapNhatThongTinViewModel
            {
                HoTen = khachHang.HoTen,
                Sdt = khachHang.Sdt,
                Email = khachHang.Email,
                Cccd = khachHang.Cccd
            };

            return View(model);
        }

        // POST: Lưu thông tin cá nhân đã chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ClientCapNhatThongTinViewModel model)
        {
            // Kiểm tra dữ liệu nhập
            if (!ModelState.IsValid)
                return View(model);

            // Xác định tài khoản đang đăng nhập
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // Lấy hồ sơ cần cập nhật từ database
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(
                    k => k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return NotFound();

            // Chỉ cập nhật những trường cho phép chỉnh sửa
            khachHang.HoTen = model.HoTen.Trim();
            khachHang.Sdt = model.Sdt?.Trim();
            khachHang.Email = model.Email?.Trim();
            khachHang.Cccd = model.Cccd?.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cập nhật thông tin cá nhân thành công.";

            return RedirectToAction(nameof(Index));
        }

    }
}

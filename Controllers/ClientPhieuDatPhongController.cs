
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Models.Data;
using System.Security.Claims;

namespace QuanLyKhachSan.Controllers
{
    // Lịch sử đặt phòng dành riêng cho khách hàng
    [Authorize(Roles = "KhachHang")]
    public class ClientPhieuDatPhongController : Controller
    {
        private readonly QLKhachSanContext _context;

        public ClientPhieuDatPhongController(
            QLKhachSanContext context)
        {
            _context = context;
        }

        // GET: Danh sách phiếu đặt phòng của khách hàng
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy mã tài khoản đang đăng nhập
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // Tìm hồ sơ khách hàng tương ứng với tài khoản
            var khachHang = await _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    kh => kh.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return Forbid();

            // Chỉ lấy những phiếu thuộc khách hàng hiện tại
            var dsPhieuDatPhong = await _context.PhieuDatPhongs
                .AsNoTracking()
                .Where(p => p.MaKhachHang == khachHang.MaKhachHang)
                .Include(p => p.LoaiPhong)
                .OrderByDescending(p => p.NgayDat)
                .ToListAsync();

            return View(dsPhieuDatPhong);
        }

        // POST: Khách hàng hủy phiếu đặt phòng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int maPhieu)
        {
            // 1. Xác định tài khoản đang đăng nhập
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // 2. Lấy hồ sơ khách hàng của tài khoản hiện tại
            var khachHang = await _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    kh => kh.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return Forbid();

            // 3. Chỉ tìm phiếu thuộc chính khách hàng này
            var phieuDatPhong = await _context.PhieuDatPhongs
                .FirstOrDefaultAsync(p =>
                    p.MaPhieu == maPhieu &&
                    p.MaKhachHang == khachHang.MaKhachHang);

            if (phieuDatPhong == null)
                return NotFound();

            // 4. Chỉ cho hủy khi phiếu đang chờ xác nhận
            if (phieuDatPhong.TrangThai != "Chờ xác nhận")
            {
                TempData["ErrorMessage"] =
                    "Chỉ có thể hủy phiếu đang chờ xác nhận.";

                return RedirectToAction(nameof(Index));
            }

            // 5. Cập nhật trạng thái phiếu
            phieuDatPhong.TrangThai = "Đã hủy";

            await _context.SaveChangesAsync();

            // 6. Thông báo kết quả
            TempData["SuccessMessage"] =
                $"Đã hủy phiếu đặt phòng #{phieuDatPhong.MaPhieu}.";

            return RedirectToAction(nameof(Index));
        }
    }
}

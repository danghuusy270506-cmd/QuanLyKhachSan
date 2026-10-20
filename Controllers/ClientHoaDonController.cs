
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using System.Security.Claims;

namespace QuanLyKhachSan.Controllers
{
    // Trang hóa đơn dành riêng cho khách hàng
    [Authorize(Roles = "KhachHang")]
    public class ClientHoaDonController : Controller
    {
        private readonly QLKhachSanContext _context;

        public ClientHoaDonController(
            QLKhachSanContext context)
        {
            _context = context;
        }

        // GET: Danh sách hóa đơn của khách hàng hiện tại
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Lấy mã tài khoản đang đăng nhập
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // 2. Tìm hồ sơ khách hàng của tài khoản
            var maKhachHang = await _context.KhachHangs
                .AsNoTracking()
                .Where(k => k.MaTaiKhoan == maTaiKhoan)
                .Select(k => (int?)k.MaKhachHang)
                .FirstOrDefaultAsync();

            if (!maKhachHang.HasValue)
                return Forbid();

            // 3. Chỉ lấy hóa đơn thuộc các phiếu của khách này
            var danhSachHoaDon = await _context.HoaDons
                .AsNoTracking()
                .Include(hd => hd.PhieuDatPhong)
                    .ThenInclude(p => p!.LoaiPhong)
                .Where(hd =>
                    hd.PhieuDatPhong != null &&
                    hd.PhieuDatPhong.MaKhachHang
                        == maKhachHang.Value)
                .OrderByDescending(hd => hd.MaPhieu)
                .ToListAsync();

            return View(danhSachHoaDon);
        }

        // GET: Chi tiết hóa đơn của khách hàng
        [HttpGet]
        public async Task<IActionResult> Details(string maHoaDon)
        {
            if (string.IsNullOrWhiteSpace(maHoaDon))
                return NotFound();

            // Xác định tài khoản đang đăng nhập
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // Lấy hồ sơ khách hàng hiện tại
            var khachHang = await _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    k => k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return Forbid();

            // Chỉ lấy hóa đơn thuộc phiếu của khách hàng này
            var hoaDon = await _context.HoaDons
                .AsNoTracking()
                .Include(hd => hd.PhieuDatPhong)
                    .ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(hd =>
                    hd.MaHoaDon == maHoaDon &&
                    hd.PhieuDatPhong != null &&
                    hd.PhieuDatPhong.MaKhachHang
                        == khachHang.MaKhachHang);

            if (hoaDon == null)
                return NotFound();

            return View(hoaDon);
        }

    }
}

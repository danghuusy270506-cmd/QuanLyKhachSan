using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;

namespace QuanLyKhachSan.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/hoadon")]
    public class HoaDonApiController : ControllerBase
    {
        private readonly QLKhachSanContext _context;

        public HoaDonApiController(QLKhachSanContext context)
        {
            _context = context;
        }

        // GET: /api/hoadon
        // GET: /api/hoadon?tuKhoa=HD1&trangThai=DaThanhToan&page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetDanhSach(
            string? tuKhoa,
            string? trangThai,
            int page = 1,
            int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.HoaDons
                .AsNoTracking()
                .AsQueryable();

            // Khách hàng chỉ xem hóa đơn của mình
            if (User.IsInRole("KhachHang"))
            {
                var userIdStr =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdStr, out int maTaiKhoan))
                    return Forbid();

                var maKhachHang = await _context.KhachHangs
                    .Where(k => k.MaTaiKhoan == maTaiKhoan)
                    .Select(k => (int?)k.MaKhachHang)
                    .FirstOrDefaultAsync();

                if (!maKhachHang.HasValue)
                    return Forbid();

                query = query.Where(hd =>
                    hd.PhieuDatPhong != null &&
                    hd.PhieuDatPhong.MaKhachHang == maKhachHang.Value);
            }
            else if (!User.IsInRole("NhanVien") &&
                     !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();

                bool laMaPhieu = int.TryParse(tuKhoa, out int maPhieu);

                query = query.Where(hd =>
                    hd.MaHoaDon.Contains(tuKhoa) ||
                    (laMaPhieu && hd.MaPhieu == maPhieu));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(hd => hd.TrangThai == trangThai);
            }

            int tongBanGhi = await query.CountAsync();

            var danhSach = await query
                .OrderByDescending(hd => hd.MaPhieu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(hd => new
                {
                    hd.MaHoaDon,
                    hd.MaPhieu,
                    hd.TongTien,
                    hd.TienCoc,
                    hd.GiamGia,
                    hd.KieuThanhToan,
                    hd.ThoiGianThanhToan,
                    hd.TrangThai
                })
                .ToListAsync();

            return Ok(new
            {
                thanhCong = true,
                page,
                pageSize,
                tongBanGhi,
                tongTrang = (int)Math.Ceiling(
                    tongBanGhi / (double)pageSize),
                data = danhSach
            });
        }

        // GET: /api/hoadon/HD1
        [HttpGet("{maHoaDon}")]
        public async Task<IActionResult> GetChiTiet(
    string maHoaDon)
        {
            var query = _context.HoaDons
                .AsNoTracking()
                .AsQueryable();

            // Xác định giới hạn dữ liệu theo Role
            if (User.IsInRole("KhachHang"))
            {
                var userIdStr =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdStr, out int maTaiKhoan))
                    return Forbid();

                var maKhachHang = await _context.KhachHangs
                    .Where(k => k.MaTaiKhoan == maTaiKhoan)
                    .Select(k => (int?)k.MaKhachHang)
                    .FirstOrDefaultAsync();

                if (!maKhachHang.HasValue)
                    return Forbid();

                query = query.Where(hd =>
                    hd.PhieuDatPhong != null &&
                    hd.PhieuDatPhong.MaKhachHang == maKhachHang.Value);
            }
            else if (!User.IsInRole("NhanVien") &&
                     !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var hoaDon = await query
                .Where(hd => hd.MaHoaDon == maHoaDon)
                .Select(hd => new
                {
                    hd.MaHoaDon,
                    hd.MaPhieu,
                    hd.TongTien,
                    hd.TienCoc,
                    hd.GiamGia,
                    hd.KieuThanhToan,
                    hd.ThoiGianThanhToan,
                    hd.TrangThai
                })
                .FirstOrDefaultAsync();

            if (hoaDon == null)
            {
                return NotFound(new
                {
                    thanhCong = false,
                    thongBao = "Không tìm thấy hóa đơn."
                });
            }

            return Ok(new
            {
                thanhCong = true,
                data = hoaDon
            });
        }
    }
}

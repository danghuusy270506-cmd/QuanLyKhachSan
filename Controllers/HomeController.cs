using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    [Authorize] // Yêu cầu người dùng phải đăng nhập
    public class HomeController : Controller
    {
        private readonly QLKhachSanContext _context;

        public HomeController(QLKhachSanContext context)
        {
            _context = context;
        }

        // Trang Dashboard Quản trị (Chỉ dành cho Admin và NhanVien)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Nếu người đăng nhập là Khách hàng -> Chuyển hướng sang trang đặt phòng/loại phòng
            if (User.IsInRole("KhachHang"))
            {
                return RedirectToAction("Index", "LoaiPhong");
            }

            // Nhân viên -> trang Staff
            if (User.IsInRole("NhanVien"))
            {
                return RedirectToAction("StaffIndex", "Home");
            }


            var homNay = DateTime.Today;
            var ngayMai = homNay.AddDays(1);
            var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
            var dau7Ngay = homNay.AddDays(-6);

            var phong = await _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.Tang)
                .ThenBy(p => p.SoPhong)
                .ToListAsync();

            var hoaDonHomNay = await _context.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThai == "DaThanhToan"
                    && h.ThoiGianThanhToan >= homNay
                    && h.ThoiGianThanhToan < ngayMai)
                .Select(h => (decimal)h.TongTien)
                .ToListAsync();

            var hoaDonThangNay = await _context.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThai == "DaThanhToan"
                    && h.ThoiGianThanhToan >= dauThang
                    && h.ThoiGianThanhToan < ngayMai)
                .Select(h => (decimal)h.TongTien)
                .ToListAsync();

            var hoaDon7Ngay = await _context.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThai == "DaThanhToan"
                    && h.ThoiGianThanhToan >= dau7Ngay
                    && h.ThoiGianThanhToan < ngayMai)
                .Select(h => new { h.ThoiGianThanhToan, h.TongTien })
                .ToListAsync();

            var doanhThu7Ngay = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var ngay = dau7Ngay.AddDays(i);
                    var doanhThu = hoaDon7Ngay
                        .Where(h => h.ThoiGianThanhToan.Date == ngay.Date)
                        .Sum(h => (decimal)h.TongTien);

                    return new DoanhThuNgayViewModel
                    {
                        Ngay = ngay,
                        DoanhThu = doanhThu
                    };
                })
                .ToList();

            var doanhThuCaoNhat = doanhThu7Ngay.Max(x => x.DoanhThu);
            foreach (var item in doanhThu7Ngay)
            {
                // Sửa lỗi tính phần trăm chiều cao cột biểu đồ
                item.ChieuCaoCot = doanhThuCaoNhat <= 0
                    ? 4
                    : Math.Max(4, (double)(item.DoanhThu / doanhThuCaoNhat * 100));
            }

            var model = new DashboardViewModel
            {
                TongSoPhong = phong.Count,
                PhongTrong = phong.Count(p => p.TrangThai == "Trống"),
                PhongDangO = phong.Count(p => p.TrangThai == "Đang ở" || p.TrangThai == "Đang sử dụng"),
                PhongDangDon = phong.Count(p => p.TrangThai == "Đang dọn" || p.TrangThai == "Đang dọn dẹp"),
                TongKhachHang = await _context.KhachHangs.AsNoTracking().CountAsync(),
                TongNhanVien = await _context.NhanViens.AsNoTracking().CountAsync(),
                PhieuChoXacNhan = await _context.PhieuDatPhongs.AsNoTracking().CountAsync(p => p.TrangThai == "Chờ xác nhận"),
                PhieuDatHomNay = await _context.PhieuDatPhongs.AsNoTracking().CountAsync(p => p.NgayDat >= homNay && p.NgayDat < ngayMai),
                CheckInHomNay = await _context.ChiTietXepPhongs.AsNoTracking().CountAsync(ct => ct.NgayNhanPhongThucTe.HasValue && ct.NgayNhanPhongThucTe >= homNay && ct.NgayNhanPhongThucTe < ngayMai),
                CheckOutHomNay = await _context.ChiTietXepPhongs.AsNoTracking().CountAsync(ct => ct.NgayTra.HasValue && ct.NgayTra >= homNay && ct.NgayTra < ngayMai),
                DoanhThuHomNay = hoaDonHomNay.Sum(),
                DoanhThuThangNay = hoaDonThangNay.Sum(),
                DoanhThu7Ngay = doanhThu7Ngay,
                PhieuDatGanDay = await _context.PhieuDatPhongs
                    .AsNoTracking()
                    .Include(p => p.KhachHang)
                    .Include(p => p.LoaiPhong)
                    .OrderByDescending(p => p.NgayDat)
                    .ThenByDescending(p => p.MaPhieu)
                    .Take(6)
                    .ToListAsync(),
                PhongGanDay = phong.Take(12).ToList()
            };

            ViewData["Title"] = "Tổng quan quản trị";
            return View(model);
        }

        public IActionResult ClientIndex()
        {
            return View();
        }

        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> StaffIndex()
        {
            var homNay = DateTime.Today;
            var ngayMai = homNay.AddDays(1);

            var model = new StaffDashboardViewModel
            {
                // 1. Đếm số liệu thật
                PhieuChoXacNhan = await _context.PhieuDatPhongs.CountAsync(p => p.TrangThai == "Chờ xác nhận"),

                // Đếm số lượng phiếu có ngày đến hôm nay (chưa check-in)
                CheckInHomNay = await _context.PhieuDatPhongs.CountAsync(p =>
                    p.NgayDen >= homNay && p.NgayDen < ngayMai && p.TrangThai == "Đã xác nhận"),

                // Đếm số lượng phòng khách dự kiến trả hôm nay (chưa check-out)
                CheckOutHomNay = await _context.ChiTietXepPhongs.CountAsync(ct =>
                    ct.NgayTra == null && ct.PhieuDatPhong.NgayDi >= homNay && ct.PhieuDatPhong.NgayDi < ngayMai),

                // Đếm hóa đơn chưa thanh toán
                HoaDonChuaThanhToan = await _context.HoaDons.CountAsync(h => h.TrangThai == "ChuaThanhToan"),


                // 2. Lấy danh sách Phiếu đặt gần đây (thay thế cho phần Việc cần làm)
                PhieuGanDay = await _context.PhieuDatPhongs
                    .Include(p => p.KhachHang)
                    .Include(p => p.LoaiPhong)
                    .OrderByDescending(p => p.NgayDat)
                    .Take(5)
                    .ToListAsync(),

                // 3. Lấy sơ đồ phòng
                PhongHomNay = await _context.Phongs
                    .Include(p => p.LoaiPhong)
                    .OrderBy(p => p.Tang).ThenBy(p => p.SoPhong)
                    .ToListAsync()
            };

            ViewData["Title"] = "Tổng quan Lễ tân";
            return View(model);
        }

        // --- API AJAX: Xác nhận thanh toán hóa đơn ---
        [HttpPost]
        [Authorize(Roles = "Admin,NhanVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToan(string maHoaDon)
        {
            var hoaDon = await _context.HoaDons
                .Include(hd => hd.PhieuDatPhong)
                .ThenInclude(p => p.ChiTietXepPhongs)
                .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);

            if (hoaDon == null || hoaDon.TrangThai == "DaThanhToan")
                return Json(new { success = false, message = "Hóa đơn không hợp lệ hoặc đã thanh toán." });

            hoaDon.TrangThai = "DaThanhToan";
            hoaDon.ThoiGianThanhToan = DateTime.Now;

            if (hoaDon.PhieuDatPhong != null)
            {
                hoaDon.PhieuDatPhong.TrangThai = "Hoàn thành";
                foreach (var ct in hoaDon.PhieuDatPhong.ChiTietXepPhongs)
                {
                    if (ct.Phong != null && ct.NgayTra != null)
                        ct.Phong.TrangThai = "Đang dọn";
                }
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // --- API AJAX: Duyệt phiếu đặt phòng ---
        [HttpPost]
        [Authorize(Roles = "Admin,NhanVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanPhieu(int maPhieu)
        {
            var phieu = await _context.PhieuDatPhongs.FindAsync(maPhieu);
            if (phieu == null || phieu.TrangThai != "Chờ xác nhận")
                return Json(new { success = false, message = "Phiếu không hợp lệ hoặc đã được xử lý." });

            phieu.TrangThai = "Đã xác nhận";
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [AllowAnonymous] // Ai cũng có thể xem trang chính sách bảo mật
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
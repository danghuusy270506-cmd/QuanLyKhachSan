using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;

namespace QuanLyKhachSan.Controllers
{
    // Bắt buộc đăng nhập để truy cập Controller
    [Authorize]
    public class DichVuController : Controller
    {
        private readonly QLKhachSanContext _context;

        public DichVuController(QLKhachSanContext context)
        {
            _context = context;
        }

        // GET: /DichVu (Tất cả người dùng đăng nhập đều xem được)
        [HttpGet]
        public async Task<IActionResult> Index(string? tuKhoa)
        {
            var truyVan = _context.DichVus.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();
                truyVan = truyVan.Where(dv => dv.TenDichVu.Contains(tuKhoa)
                    || (dv.MoTa != null && dv.MoTa.Contains(tuKhoa))
                    || dv.MaDichVu.Contains(tuKhoa));
            }

            var danhSach = await truyVan.OrderBy(dv => dv.TenDichVu).ToListAsync();
            ViewData["TuKhoa"] = tuKhoa;

            // Kiểm tra nếu request là AJAX thì trả về PartialView
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Có thể trả về JSON chứa HTML và tổng số lượng, hoặc dùng ViewBag/ViewData
                ViewBag.TotalCount = danhSach.Count;
                return PartialView("_DichVuPartial", danhSach);
            }

            return View(danhSach);
        }

        // GET: /DichVu/Details/DV001 (Tất cả người dùng đăng nhập đều xem được)
        [HttpGet]
        public async Task<IActionResult> Details(string? madichvu)
        {
            if (string.IsNullOrWhiteSpace(madichvu)) return NotFound();

            var dichVu = await _context.DichVus.AsNoTracking()
                .FirstOrDefaultAsync(dv => dv.MaDichVu == madichvu);
            return dichVu == null ? NotFound() : View(dichVu);
        }

        // GET: /DichVu/Create (Chỉ Admin và NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public IActionResult Create() => View(new DichVu());

        // POST: /DichVu/Create (Chỉ Admin và NhanVien)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create([Bind("TenDichVu,MoTa,DonGia")] DichVu dichVu)
        {
            if (!ModelState.IsValid) return View(dichVu);

            // Sinh mã DVxxx dựa trên mã lớn nhất hiện có.
            var cacMa = await _context.DichVus.AsNoTracking()
                .Select(dv => dv.MaDichVu).ToListAsync();
            var soTiepTheo = cacMa
                .Where(ma => !string.IsNullOrWhiteSpace(ma)
                    && ma.StartsWith("DV", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(ma.Substring(2), out _))
                .Select(ma => int.Parse(ma.Substring(2)))
                .DefaultIfEmpty(0).Max() + 1;

            dichVu.MaDichVu = $"DV{soTiepTheo:D3}";
            _context.DichVus.Add(dichVu);
            await _context.SaveChangesAsync();
            TempData["ThongBaoThanhCong"] = $"Đã thêm dịch vụ '{dichVu.TenDichVu}'.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /DichVu/Edit/DV001 (Chỉ Admin và NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(string? madichvu)
        {
            if (string.IsNullOrWhiteSpace(madichvu)) return NotFound();
            var dichVu = await _context.DichVus.FindAsync(madichvu);
            return dichVu == null ? NotFound() : View(dichVu);
        }

        // POST: /DichVu/Edit/DV001 (Chỉ Admin và NhanVien)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(string? madichvu, [Bind("MaDichVu,TenDichVu,MoTa,DonGia")] DichVu duLieuGuiLen)
        {
            if (string.IsNullOrWhiteSpace(madichvu) || madichvu != duLieuGuiLen.MaDichVu)
                return NotFound();

            if (!ModelState.IsValid) return View(duLieuGuiLen);

            var dichVu = await _context.DichVus.FindAsync(madichvu);
            if (dichVu == null) return NotFound();

            // Chỉ cập nhật các trường cho phép
            dichVu.TenDichVu = duLieuGuiLen.TenDichVu.Trim();
            dichVu.MoTa = duLieuGuiLen.MoTa?.Trim();
            dichVu.DonGia = duLieuGuiLen.DonGia;

            await _context.SaveChangesAsync();
            TempData["ThongBaoThanhCong"] = $"Đã cập nhật dịch vụ '{dichVu.TenDichVu}'.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /DichVu/Delete/DV001 (Chỉ Admin và NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Delete(string? madichvu)
        {
            if (string.IsNullOrWhiteSpace(madichvu)) return NotFound();
            var dichVu = await _context.DichVus.AsNoTracking()
                .FirstOrDefaultAsync(dv => dv.MaDichVu == madichvu);
            return dichVu == null ? NotFound() : View(dichVu);
        }

        // POST: /DichVu/Delete/DV001 (Chỉ Admin và NhanVien)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> DeleteConfirmed(string? madichvu)
        {
            if (string.IsNullOrWhiteSpace(madichvu)) return NotFound();
            var dichVu = await _context.DichVus.FindAsync(madichvu);
            if (dichVu == null) return NotFound();

            // Dịch vụ đã xuất hiện trên hóa đơn/lượt lưu trú thì không xóa để giữ lịch sử.
            var daDuocSuDung = await _context.ChiTietXepPhong_DichVus
                .AnyAsync(ct => ct.MaDichVu == madichvu);
            if (daDuocSuDung)
            {
                TempData["ThongBaoLoi"] = $"Không thể xóa dịch vụ '{dichVu.TenDichVu}' vì đã có phát sinh sử dụng. Bạn có thể sửa tên hoặc đơn giá cho các lần sử dụng sau.";
                return RedirectToAction(nameof(Index));
            }

            _context.DichVus.Remove(dichVu);
            await _context.SaveChangesAsync();
            TempData["ThongBaoThanhCong"] = $"Đã xóa dịch vụ '{dichVu.TenDichVu}'.";
            return RedirectToAction(nameof(Index));
        }
    }
}
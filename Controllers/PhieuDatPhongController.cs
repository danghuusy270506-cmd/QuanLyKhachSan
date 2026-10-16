using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    public class PhieuDatPhongController : Controller
    {
        private readonly QLKhachSanContext _context;

        public PhieuDatPhongController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. DSN PHIẾU ĐẶT PHÒNG
        public async Task<IActionResult> Index()
        {
            var dsPhieu = _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.LoaiPhong)
                .Include(p => p.NhanVien);

            return View(await dsPhieu.ToListAsync());
        }

        // 2. CHI TIẾT PHIẾU ĐẶT PHÒNG
        public async Task<IActionResult> Details(int? maphieu)
        {
            if (maphieu == null) return NotFound();

            var phieudatphong = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.LoaiPhong)
                .Include(p => p.NhanVien)
                .FirstOrDefaultAsync(m => m.MaPhieu == maphieu);

            if (phieudatphong == null) return NotFound();

            return View(phieudatphong);
        }

        // 3. GIAO DIỆN ĐẶT PHÒNG (GET: Create)
        public IActionResult Create()
        {
            // Nạp danh sách Loại phòng ra DropDownList
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong");
            return View();
        }

        // 4. XỬ LÝ ĐẶT PHÒNG (POST: Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NgayDen,NgayDi,TienCoc,SoNguoi,MaLoaiPhong")] PhieuDatPhong phieudatphong)
        {
            // Lấy ID tài khoản đang đăng nhập từ JWT Cookie Claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int maTaiKhoan))
            {
                return RedirectToAction("Authentication", "DangNhap");
            }

            // Tự động gán các thông tin mặc định
            phieudatphong.MaKhachHang = maTaiKhoan;
            phieudatphong.NgayDat = DateTime.Now;
            phieudatphong.TrangThai = "Chờ xác nhận";

            // Validate logic ngày đến & ngày đi
            if (phieudatphong.NgayDi <= phieudatphong.NgayDen)
            {
                ModelState.AddModelError("NgayDi", "Ngày đi phải sau ngày đến.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(phieudatphong);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieudatphong.MaLoaiPhong);
            return View(phieudatphong);
        }

        // 5. GIAO DIỆN SỬA PHIẾU (GET: Edit)
        public async Task<IActionResult> Edit(int? maphieu)
        {
            if (maphieu == null) return NotFound();

            var phieudatphong = await _context.PhieuDatPhongs.FindAsync(maphieu);
            if (phieudatphong == null) return NotFound();

            ViewData["MaKhachHang"] = new SelectList(_context.KhachHangs, "MaKhachHang", "HoTen", phieudatphong.MaKhachHang);
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieudatphong.MaLoaiPhong);
            return View(phieudatphong);
        }

        // 6. XỬ LÝ SỬA PHIẾU (POST: Edit)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int maphieu, [Bind("MaPhieu,NgayDat,NgayDen,NgayDi,TienCoc,SoNguoi,TrangThai,MaKhachHang,MaNhanVien,MaLoaiPhong")] PhieuDatPhong phieudatphong)
        {
            if (maphieu != phieudatphong.MaPhieu) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phieudatphong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhieuDatPhongExists(phieudatphong.MaPhieu)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaKhachHang"] = new SelectList(_context.KhachHangs, "MaKhachHang", "HoTen", phieudatphong.MaKhachHang);
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieudatphong.MaLoaiPhong);
            return View(phieudatphong);
        }

        // 7. GIAO DIỆN XÓA PHIẾU (GET: Delete)
        public async Task<IActionResult> Delete(int? maphieu)
        {
            if (maphieu == null) return NotFound();

            var phieudatphong = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(m => m.MaPhieu == maphieu);

            if (phieudatphong == null) return NotFound();

            return View(phieudatphong);
        }

        // 8. XỬ LÝ XÓA PHIẾU (POST: Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int maphieu)
        {
            var phieudatphong = await _context.PhieuDatPhongs.FindAsync(maphieu);
            if (phieudatphong != null)
            {
                _context.PhieuDatPhongs.Remove(phieudatphong);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PhieuDatPhongExists(int maphieu)
        {
            return _context.PhieuDatPhongs.Any(e => e.MaPhieu == maphieu);
        }
    }
}
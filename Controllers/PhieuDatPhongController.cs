using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System.Linq;
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

        // 1. DS PHIẾU ĐẶT PHÒNG
        public async Task<IActionResult> Index(int? page, string maPhieu, string khachHang, string trangThai, int? maLoaiPhong, DateTime? ngayDen, DateTime? ngayDi)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;
            var query = _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.LoaiPhong)
                .Include(p => p.NhanVien)
                .AsQueryable();

            //Lọc && Tìm kiếm
            if (!string.IsNullOrEmpty(maPhieu))
            {
                string ma = maPhieu.Replace("#", "").Trim();
                if(int.TryParse(ma, out int maPhieuDatPhong))
                {
                    query = query.Where(p => p.MaPhieu == maPhieuDatPhong);
                }
            }
            if (!string.IsNullOrEmpty(khachHang))
            {
                string keyWord = khachHang.Trim().ToLower();
                query = query.Where(p => p.KhachHang != null && p.KhachHang.HoTen.ToLower().Contains(keyWord));
            }
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }
            if(maLoaiPhong.HasValue && maLoaiPhong.Value > 0)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong.Value);
            }
            if (ngayDen.HasValue)
            {
                query = query.Where(p => p.NgayDen.Date >= ngayDen.Value.Date);
            }
            if (ngayDi.HasValue)
            {
                query = query.Where(p => p.NgayDi.Date <= ngayDi.Value.Date);
            }

            query = query.OrderByDescending(p => p.MaPhieu);
            //Tổng số lượng phiếu đặt phòng để tính số trang
            int soLuongPhieu = await query.CountAsync();
            int tongSoTrang = (int)Math.Ceiling(soLuongPhieu / (double)pageSize);
            // Lấy dữ liệu của trang hiện tại
            var dsPhieu = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            // Truyền thông tin phân trang sang View
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = tongSoTrang;

            ViewBag.maPhieu = maPhieu;
            ViewBag.khachHang = khachHang;
            ViewBag.trangThai = trangThai;
            ViewBag.maLoaiPhong = maLoaiPhong;
            ViewBag.ngayDen = ngayDen?.ToString("yyyy-MM-dd");
            ViewBag.ngayDi = ngayDi?.ToString("yyyy-MM-dd");
            ViewData["ListLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", maLoaiPhong);
            ViewData["ListTrangThai"] = new SelectList(new List<string>
            {
                "Chờ xác nhận", "Đã xác nhận", "Đang ở", "Hoàn thành", "Đã hủy"
            }, trangThai);

            //Ajax
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PhieuDatPhongPartial", dsPhieu);
            }
            return View(dsPhieu);
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

            return PartialView("_DetailsPartial", phieudatphong);
        }

        // 3. GIAO DIỆN ĐẶT PHÒNG (GET: Create)
        public IActionResult Create()
        {
            // Kiểm tra khách hàng đã đăng nhập chưa
            int? maKhachHang = HttpContext.Session.GetInt32("MaKhachHang");
            if (maKhachHang == null)
            {
                // Trả về một PartialView thông báo lỗi hoặc JSON có cấu trúc để JS xử lý thông báo
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Content("<div class='modal-header bg-danger text-white'><h5 class='modal-title'>Thông báo</h5><button type='button' class='btn-close btn-close-white' data-bs-dismiss='modal'></button></div><div class='modal-body p-4 text-center'><h6>Vui lòng đăng nhập tài khoản khách hàng để thực hiện đặt phòng!</h6></div>", "text/html");
                }
                return RedirectToAction("Authentication", "DangNhap");
            }
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong");

            var phieuMoi = new PhieuDatPhong();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", phieuMoi);
            }
            return View(phieuMoi);
        }

        // 4. XỬ LÝ ĐẶT PHÒNG (POST: Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NgayDen,NgayDi,TienCoc,SoNguoi,MaLoaiPhong")] PhieuDatPhong phieudatphong)
        {
            int? maKhachHang = HttpContext.Session.GetInt32("MaKhachHang");
            if (maKhachHang == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
                }
                return RedirectToAction("Authentication", "DangNhap");
            }

            // Tự động gán các thông tin mặc định
            phieudatphong.MaKhachHang = maKhachHang.Value;
            phieudatphong.NgayDat = System.DateTime.Now;
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

                // Nếu là AJAX gửi lên, trả về JSON thành công để JS đóng modal và load lại bảng
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieudatphong.MaLoaiPhong);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", phieudatphong);
            }
            return View(phieudatphong);
        }

        // 5. GIAO DIỆN SỬA PHIẾU (GET: Edit)
        public async Task<IActionResult> Edit(int? maphieu)
        {
            if (maphieu == null) return NotFound();

            var phieuDatPhong = await _context.PhieuDatPhongs.FindAsync(maphieu);
            if (phieuDatPhong == null) return NotFound();

            ViewData["MaKhachHang"] = new SelectList(_context.KhachHangs, "MaKhachHang", "HoTen", phieuDatPhong.MaKhachHang);
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieuDatPhong.MaLoaiPhong);
            return PartialView("_EditPartial", phieuDatPhong);
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
                    var existingPhieu = await _context.PhieuDatPhongs
                    .Include(p => p.KhachHang)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.NhanVien)
                    .FirstOrDefaultAsync(p => p.MaPhieu == maphieu);

                    if (existingPhieu == null) return NotFound();

                    // Cập nhật các trường dữ liệu mới từ form
                    existingPhieu.MaKhachHang = phieudatphong.MaKhachHang;
                    existingPhieu.MaLoaiPhong = phieudatphong.MaLoaiPhong;
                    existingPhieu.NgayDen = phieudatphong.NgayDen;
                    existingPhieu.NgayDi = phieudatphong.NgayDi;
                    existingPhieu.SoNguoi = phieudatphong.SoNguoi;
                    existingPhieu.TienCoc = phieudatphong.TienCoc;
                    existingPhieu.TrangThai = phieudatphong.TrangThai;

                    _context.Update(existingPhieu);
                    await _context.SaveChangesAsync();

                    // Trả về JSON báo thành công để JavaScript đóng modal và load lại bảng
                    return Json(new { success = true });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhieuDatPhongExists(phieudatphong.MaPhieu)) return NotFound();
                    else throw;
                }
            }

            ViewData["MaKhachHang"] = new SelectList(_context.KhachHangs, "MaKhachHang", "HoTen", phieudatphong.MaKhachHang);
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieudatphong.MaLoaiPhong);
            return PartialView("_EditPartial", phieudatphong);
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

            return PartialView("_DeletePartial", phieudatphong);
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
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng để xóa." });
        }

        private bool PhieuDatPhongExists(int maphieu)
        {
            return _context.PhieuDatPhongs.Any(e => e.MaPhieu == maphieu);
        }

        // XỬ LÝ ĐỔI TRẠNG THÁI SANG "ĐÃ XÁC NHẬN"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhan(int maphieu)
        {
            var phieu = await _context.PhieuDatPhongs.FindAsync(maphieu);
            if (phieu == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
            }

            phieu.TrangThai = "Đã xác nhận";
            _context.Update(phieu);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
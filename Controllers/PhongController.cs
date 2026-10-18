using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    [Authorize]
    public class PhongController : Controller
    {
        private readonly QLKhachSanContext _context;

        public PhongController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH PHÒNG
        [HttpGet]
        public async Task<IActionResult> Index(string soPhong,string tenLoaiPhong, string trangThai, int page = 1, int pageSize=10)
        {
            var query = _context.Phongs.Include(p => p.LoaiPhong).AsQueryable();
            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(soPhong))
            {
                query = query.Where(p => p.SoPhong.Contains(soPhong));
            }
            // 2. Lọc theo Loại phòng
            if (!string.IsNullOrEmpty(tenLoaiPhong) && tenLoaiPhong != "Tất cả")
            {
                query = query.Where(p => p.LoaiPhong != null && p.LoaiPhong.TenLoaiPhong.Contains(tenLoaiPhong));
            }
            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }
            // Tổng số bản ghi phục vụ phân trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy dữ liệu phân trang
            var dsPhong = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Truyền dữ liệu phân trang qua ViewBag để sử dụng ở View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.soPhong = soPhong;
            ViewBag.tenLoaiPhong = tenLoaiPhong;
            ViewBag.TrangThai = trangThai;

            // Lấy danh sách tên loại phòng duy nhất từ CSDL cho Dropdown
            ViewBag.DanhSachLoaiPhong = await _context.LoaiPhongs
                .Select(l => l.TenLoaiPhong)
                .Distinct()
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PhongPartial", dsPhong);
            }

            return View(dsPhong);
        }

        // 2. CHI TIẾT PHÒNG (Tất cả người dùng đăng nhập đều có thể xem)
        [HttpGet] 
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(m => m.MaPhong == id);

            if (phong == null) return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DetailsPartial", phong);
            }

            return View(phong);
        }

        // 3. GIAO DIỆN THÊM PHÒNG (Chỉ Admin và NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public IActionResult Create()
        {
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", new Phong());
            }
            return View();
        }

        // 4. XỬ LÝ THÊM PHÒNG (Chỉ Admin và NhanVien)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create([Bind("SoPhong,Tang,TrangThai,MaLoaiPhong")] Phong phong)
        {
            if (ModelState.IsValid)
            {
                _context.Add(phong);
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", phong);
            }
            return View(phong);
        }
        
        // 5. GIAO DIỆN SỬA PHÒNG (Chỉ Admin và NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var phong = await _context.Phongs.FindAsync(id);
            if (phong == null) return NotFound();

            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", phong);
            }
            return View(phong);
        }

        // 6. XỬ LÝ SỬA PHÒNG (Chỉ Admin và NhanVien)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhong,SoPhong,Tang,TrangThai,MaLoaiPhong")] Phong phong)
        {
            if (id != phong.MaPhong) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phong);
                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true});
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhongExists(phong.MaPhong)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", phong);
            }

            return View(phong);
        }

        // 7. GIAO DIỆN XÓA PHÒNG (Chỉ Admin và NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(m => m.MaPhong == id);

            if (phong == null) return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DeletePartial", phong);
            }

            return View(phong);
        }


        // 8. XỬ LÝ XÓA PHÒNG (Chỉ Admin và NhanVien)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phong = await _context.Phongs.FindAsync(id);
            if (phong != null)
            {
                _context.Phongs.Remove(phong);
                await _context.SaveChangesAsync();
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PhongExists(int id)
        {
            return _context.Phongs.Any(e => e.MaPhong == id);
        }
    }
}
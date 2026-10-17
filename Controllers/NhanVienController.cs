using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;

namespace QuanLyKhachSan.Controllers
{
    [Authorize]
    [Authorize(Roles = "ADMIN")]
    public class NhanVienController : Controller
    {
        private readonly QLKhachSanContext _context;

        public NhanVienController(QLKhachSanContext context)
        {
            _context = context;
        }

        

        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 5)
        {
            if (page < 1) page = 1;

            // Lấy truy vấn ban đầu
            var query = _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking();

            // Tìm kiếm theo tên hoặc SĐT/CCCD (nếu có nhập từ form)
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(nv => nv.HoTen.Contains(searchString) ||
                                          (nv.Sdt != null && nv.Sdt.Contains(searchString)) ||
                                          (nv.Cccd != null && nv.Cccd.Contains(searchString)));
            }

            // Tính tổng số lượng bản ghi để tính tổng số trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Bỏ qua các bản ghi trước đó và lấy số lượng tương ứng với pageSize
            var dsNhanVien = await query
                .OrderBy(nv => nv.MaNhanVien)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền các tham số phân trang qua ViewBag sang View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.SearchString = searchString;

            return View(dsNhanVien);
        }

        // 2. CHI TIẾT NHÂN VIÊN (DETAILS)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaNhanVien == id);

            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // 3. SỬA NHÂN VIÊN - GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // 3. SỬA NHÂN VIÊN - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaNhanVien,HoTen,Cccd,Sdt,Email,MaTaiKhoan")] NhanVien nhanVien)
        {
            if (id != nhanVien.MaNhanVien) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nhanVien);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhanVienExists(nhanVien.MaNhanVien)) return NotFound();
                    else throw;
                }
            }

            return View(nhanVien);
        }

        // 4. XÓA NHÂN VIÊN - GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaNhanVien == id);

            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // 4. XÓA NHÂN VIÊN - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien != null)
            {
                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa nhân viên thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool NhanVienExists(int id)
        {
            return _context.NhanViens.Any(e => e.MaNhanVien == id);
        }
    }
}
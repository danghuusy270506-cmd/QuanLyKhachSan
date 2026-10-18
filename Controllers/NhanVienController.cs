using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    // Bắt buộc đăng nhập cho toàn bộ Controller
    [Authorize]
    public class NhanVienController : Controller
    {
        private readonly QLKhachSanContext _context;

        public NhanVienController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH NHÂN VIÊN (Chỉ dành riêng cho Admin)
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 5)
        {
            if (page < 1) page = 1;

            var query = _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking();

            // Tìm kiếm theo tên hoặc SĐT/CCCD
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(nv => nv.HoTen.Contains(searchString) ||
                                          (nv.Sdt != null && nv.Sdt.Contains(searchString)) ||
                                          (nv.Cccd != null && nv.Cccd.Contains(searchString)));
            }

            // Tính tổng số lượng bản ghi
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Phân trang
            var dsNhanVien = await query
                .OrderBy(nv => nv.MaNhanVien)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền các tham số phân trang sang View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.SearchString = searchString;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_NhanVienPartial", dsNhanVien);
            }

            return View(dsNhanVien);
        }

        // 2. CHI TIẾT NHÂN VIÊN (Admin xem bất kỳ; NhanVien chỉ xem chính mình)
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id <= 0) return NotFound("ID không hợp lệ!");

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaNhanVien == id);

            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên này!");

            // Kiểm tra quyền riêng tư: Nếu là NhanVien thì chỉ được xem chính mình
            if (User.IsInRole("NhanVien") && !IsOwner(nhanVien))
            {
                return Forbid();
            }

            // ĐOẠN NÀY ĐÃ XỬ LÝ SẴN CHO AJAX:
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DetailsPartial", nhanVien);
            }

            return View(nhanVien);
        }

        // 3. SỬA NHÂN VIÊN - GET (Admin sửa bất kỳ; NhanVien chỉ sửa chính mình)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();

            if (User.IsInRole("NhanVien") && !IsOwner(nhanVien))
            {
                return Forbid();
            }

            // Nếu là gọi AJAX thì trả về PartialView
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", nhanVien);
            }

            return View(nhanVien);
        }

        // 3. SỬA NHÂN VIÊN - POST (Admin sửa bất kỳ; NhanVien chỉ sửa chính mình)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaNhanVien,HoTen,Cccd,Sdt,Email,MaTaiKhoan")] NhanVien model)
        {
            if (id != model.MaNhanVien) return NotFound();

            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();

            if (User.IsInRole("NhanVien") && !IsOwner(nhanVien))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    nhanVien.HoTen = model.HoTen;
                    nhanVien.Cccd = model.Cccd;
                    nhanVien.Sdt = model.Sdt;
                    nhanVien.Email = model.Email;

                    _context.Update(nhanVien);
                    await _context.SaveChangesAsync();

                    // Nếu gọi qua AJAX, trả về JSON thành công để xử lý phía Client
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Cập nhật thông tin nhân viên thành công!" });
                    }

                    TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhanVienExists(nhanVien.MaNhanVien)) return NotFound();
                    else throw;
                }
            }

            // Nếu ModelState không hợp lệ khi gọi AJAX, trả lại Partial kèm lỗi
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", model);
            }

            return View(model);
        }

        // 4. XÓA NHÂN VIÊN - GET (Chỉ dành riêng cho Admin)
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaNhanVien == id);

            if (nhanVien == null) return NotFound();

            // Nếu gọi qua AJAX, trả về PartialView cho Modal
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DeletePartial", nhanVien);
            }

            return View(nhanVien);
        }

        // 4. XÓA NHÂN VIÊN - POST (Chỉ dành riêng cho Admin)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien != null)
            {
                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();

                // Trả về JSON khi gọi AJAX thành công
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Xóa nhân viên thành công!" });
                }

                TempData["SuccessMessage"] = "Xóa nhân viên thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool NhanVienExists(int id)
        {
            return _context.NhanViens.Any(e => e.MaNhanVien == id);
        }

        // kiểm tra xem tài khoản đang đăng nhập có phải chính là nhân viên này không
        private bool IsOwner(NhanVien nhanVien)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return nhanVien.MaTaiKhoan == currentUserId;
            }
            return false;
        }
    }
}
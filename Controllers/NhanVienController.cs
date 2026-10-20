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

        
        // 2. CHI TIẾT NHÂN VIÊN
        // Admin xem bất kỳ; NhanVien chỉ xem chính mình
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN,NhanVien")]
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
        [Authorize(Roles = "Admin,ADMIN,NhanVien")]
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
        [Authorize(Roles = "Admin,ADMIN,NhanVien")]
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
            bool isAjax =
                Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.MaNhanVien == id);

            if (nhanVien == null)
            {
                if (isAjax)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy nhân viên."
                    });
                }

                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == nhanVien.MaTaiKhoan);

            if (taiKhoan == null)
            {
                const string message =
                    "Không tìm thấy tài khoản liên kết. Không thể xóa an toàn.";

                if (isAjax)
                    return Json(new { success = false, message });

                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            // Không xóa tài khoản quản trị bằng chức năng xóa nhân viên
            if (string.Equals(
                taiKhoan.Role, "Admin",
                StringComparison.OrdinalIgnoreCase))
            {
                const string message =
                    "Không thể xóa tài khoản Admin tại chức năng này.";

                if (isAjax)
                    return Json(new { success = false, message });

                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra nhân viên đã tham gia xử lý phiếu đặt chưa
            bool coLichSu = await _context.PhieuDatPhongs
                .AnyAsync(p => p.MaNhanVien == id);

            if (coLichSu)
            {
                // Giữ hồ sơ để bảo toàn lịch sử nghiệp vụ,
                // nhưng khóa tài khoản để ngăn đăng nhập tiếp.
                taiKhoan.TrangThai = "Khoa";
                await _context.SaveChangesAsync();

                const string message =
                    "Nhân viên đã có lịch sử xử lý phiếu đặt. " +
                    "Hồ sơ được giữ lại và tài khoản đã bị khóa.";

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        deleted = false,
                        message
                    });
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            // Phòng trường hợp tài khoản đang liên kết sai với hồ sơ khách hàng
            bool dangGanKhachHang = await _context.KhachHangs
                .AnyAsync(k => k.MaTaiKhoan == nhanVien.MaTaiKhoan);

            if (dangGanKhachHang)
            {
                const string message =
                    "Tài khoản đang liên kết với hồ sơ khách hàng. " +
                    "Không thể xóa tự động vì cần kiểm tra lại dữ liệu.";

                if (isAjax)
                    return Json(new { success = false, message });

                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            // Không có lịch sử nghiệp vụ: xóa hồ sơ và tài khoản
            // trong cùng một transaction.
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.NhanViens.Remove(nhanVien);
                _context.TaiKhoans.Remove(taiKhoan);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        deleted = true,
                        message = "Đã xóa nhân viên và tài khoản liên quan."
                    });
                }

                TempData["SuccessMessage"] =
                    "Đã xóa nhân viên và tài khoản liên quan.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                const string message =
                    "Không thể xóa nhân viên. Hãy kiểm tra các dữ liệu liên quan.";

                if (isAjax)
                    return Json(new { success = false, message });

                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Index));
            }
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
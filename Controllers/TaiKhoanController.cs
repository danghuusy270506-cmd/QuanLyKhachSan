using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using QuanLyKhachSan.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    [Authorize]
    public class TaiKhoanController : Controller
    {
        private readonly QLKhachSanContext _context;

        public TaiKhoanController(QLKhachSanContext context)
        {
            _context = context;
        }


        // GET: /TaiKhoan/Index (Chỉ Admin)
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> Index(int? page, string keyword, string role)
        {
            int pageSize = 5;
            int pageNumber = (page ?? 1) < 1 ? 1 : (page ?? 1);

            var query = _context.TaiKhoans.AsQueryable();

            // Xử lý điều kiện lọc
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(t => t.Username.Contains(keyword));
            }
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(t => t.Role == role);
            }

            query = query.OrderBy(t => t.MaTaiKhoan);

            int totalCount = await query.CountAsync();
            var taiKhoans = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.Keyword = keyword;
            ViewBag.Role = role;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_TaiKhoanPartial", taiKhoans);
            }

            return View(taiKhoans);
        }

        // ==========================================
        // GET: TaiKhoan/Details/5 (Admin xem mọi TK, Người dùng chỉ xem TK của chính mình)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int? mataikhoan)
        {
            if (mataikhoan == null)
                return NotFound();

            var taikhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(m => m.MaTaiKhoan == mataikhoan);

            if (taikhoan == null)
                return NotFound();

            // Kiểm tra quyền riêng tư
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("ADMIN");
            if (!isAdmin)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int currentUserId))
                {
                    if (taikhoan.MaTaiKhoan != currentUserId)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return Forbid();
                }
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return View("Details", taikhoan);
            }

            return View(taikhoan);
        }

        // ==========================================
        // GET: TaiKhoan/Create (Cho phép tự đăng ký hoặc Admin tạo)
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Create()
        {
            bool isAdmin = User.Identity?.IsAuthenticated == true &&
                           (User.IsInRole("Admin") || User.IsInRole("ADMIN"));

            ViewBag.IsAdmin = isAdmin;

            if (isAdmin)
            {
                ViewBag.RoleList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "KhachHang", Text = "Khách hàng" },
                    new SelectListItem { Value = "NhanVien", Text = "Nhân viên" }
                };
            }

            return View();
        }

        // ==========================================
        // POST: TaiKhoan/Create
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoanKhachHang model)
        {
            bool isAdmin = User.Identity?.IsAuthenticated == true &&
                           (User.IsInRole("Admin") || User.IsInRole("ADMIN"));

            ViewBag.IsAdmin = isAdmin;

            // 1. Kiểm tra trùng Username trong DB
            if (await _context.TaiKhoans.AnyAsync(t => t.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
            }

            // 2. Validate Data Annotations từ ViewModel
            if (!ModelState.IsValid)
            {
                if (isAdmin)
                {
                    ViewBag.RoleList = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "KhachHang", Text = "Khách hàng" },
                        new SelectListItem { Value = "NhanVien", Text = "Nhân viên" }
                    };
                }
                return View(model);
            }

            // 3. Phân quyền thiết lập Role: Nếu không phải Admin thì ép cố định là "KhachHang"
            string assignedRole = isAdmin ? (model.Role ?? "KhachHang") : "KhachHang";

            // 4. Mở Transaction để lưu 2 bảng Atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // BƯỚC A: Tạo và lưu TaiKhoan
                var taiKhoan = new TaiKhoan
                {
                    Username = model.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password, 10),
                    Role = assignedRole,
                    TrangThai = "HoatDong"
                };

                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync(); // SQL sinh MaTaiKhoan

                // BƯỚC B: Tạo và lưu KhachHang kết nối MaTaiKhoan
                var khachHang = new KhachHang
                {
                    HoTen = model.HoTen,
                    Sdt = model.Sdt,
                    Email = model.Email,
                    Cccd = model.Cccd,
                    MaTaiKhoan = taiKhoan.MaTaiKhoan
                };

                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();

                // BƯỚC C: Commit Transaction
                await transaction.CommitAsync();

                if (isAdmin)
                {
                    TempData["Success"] = $"Tạo tài khoản thành công với vai trò: {assignedRole}!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay.";
                    return RedirectToAction("Authentication", "DangNhap");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống: " + (ex.InnerException?.Message ?? ex.Message));

                if (isAdmin)
                {
                    ViewBag.RoleList = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "KhachHang", Text = "Khách hàng" },
                        new SelectListItem { Value = "NhanVien", Text = "Nhân viên" }
                    };
                }
                return View(model);
            }
        }

        // ==========================================
        // GET: TaiKhoan/Edit/5 (Chỉ Admin)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> Edit(int? mataikhoan)
        {
            if (mataikhoan == null)
                return NotFound();

            var taikhoan = await _context.TaiKhoans.FindAsync(mataikhoan);
            if (taikhoan == null)
                return NotFound();

            // Nếu là gọi từ AJAX Modal, trả về PartialView riêng cho form Sửa
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", taikhoan);
            }

            return View(taikhoan);
        }

        // ==========================================
        // POST: TaiKhoan/Edit/5 (Chỉ Admin)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin,ADMIN")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int? mataikhoan,
            [Bind("MaTaiKhoan,Username,Password,Role,TrangThai")] TaiKhoan taikhoan)
        {
            if (mataikhoan != taikhoan.MaTaiKhoan)
                return NotFound();

            // Lấy thông tin tài khoản hiện tại từ database
            var existingAccount = await _context.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(t => t.MaTaiKhoan == mataikhoan);
            if (existingAccount == null)
                return NotFound();

            // Nếu người dùng không nhập mật khẩu mới, giữ nguyên mật khẩu cũ
            if (string.IsNullOrWhiteSpace(taikhoan.Password))
            {
                taikhoan.Password = existingAccount.Password;
                ModelState.Remove("Password"); // Xóa lỗi validation mật khẩu nếu có
            }
            else if (taikhoan.Password != existingAccount.Password)
            {
                // Nếu có nhập mật khẩu mới thì băm (hash) mật khẩu mới trước khi lưu
                taikhoan.Password = BCrypt.Net.BCrypt.HashPassword(taikhoan.Password, 10);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taikhoan);
                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Cập nhật tài khoản thành công!" });
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaiKhoanExists(taikhoan.MaTaiKhoan))
                        return NotFound();
                    throw;
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", taikhoan);
            }

            return View(taikhoan);
        }

        // ==========================================
        // GET: TaiKhoan/Delete/5 (Chỉ Admin)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> Delete(int? mataikhoan)
        {
            if (mataikhoan == null)
                return NotFound();

            var taikhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(m => m.MaTaiKhoan == mataikhoan);

            if (taikhoan == null)
                return NotFound();

            // Trả về Partial View nếu là gọi AJAX từ Modal
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DeletePartial", taikhoan);
            }

            return View(taikhoan);
        }

        // ==========================================
        // POST: TaiKhoan/Delete/5 (Chỉ Admin)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,ADMIN")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? mataikhoan)
        {
            if (mataikhoan == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản cần xóa." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var taikhoan = await _context.TaiKhoans.FindAsync(mataikhoan);
                if (taikhoan == null)
                {
                    return Json(new { success = false, message = "Tài khoản không tồn tại trong hệ thống." });
                }

                // 1. Kiểm tra và xóa dữ liệu liên quan ở bảng KhachHang trước (tránh lỗi khóa ngoại)
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTaiKhoan == mataikhoan);
                if (khachHang != null)
                {
                    _context.KhachHangs.Remove(khachHang);
                }

                var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(n => n.MaTaiKhoan == mataikhoan);
                if (nhanVien != null) { _context.NhanViens.Remove(nhanVien); }

                // 2. Tiến hành xóa tài khoản
                _context.TaiKhoans.Remove(taikhoan);
                await _context.SaveChangesAsync();

                // 3. Xác nhận hoàn tất transaction
                await transaction.CommitAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Xóa tài khoản thành công!" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string errorMsg = "Không thể xóa: " + (ex.InnerException?.Message ?? ex.Message);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMsg });
                }

                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(Index));
            }
        }

        // ==========================================
        // Helper
        // ==========================================
        private bool TaiKhoanExists(int? mataikhoan)
        {
            return _context.TaiKhoans.Any(e => e.MaTaiKhoan == mataikhoan);
        }
    }
}
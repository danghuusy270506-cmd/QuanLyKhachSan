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
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    [AllowAnonymous] // Cho phép người dùng chưa đăng nhập tự đăng ký
    public class TaiKhoanController : Controller
    {
        private readonly QLKhachSanContext _context;

        public TaiKhoanController(QLKhachSanContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET: TaiKhoan
        // ==========================================
        public async Task<IActionResult> Index()
        {
            return View(await _context.TaiKhoans.ToListAsync());
        }

        public async Task<IActionResult> Indexx(int? page)
        {
            int pageSize = 5;
            int pageNumber = (page ?? 1) < 1 ? 1 : (page ?? 1);

            var query = _context.TaiKhoans.OrderBy(t => t.MaTaiKhoan);

            int totalCount = await query.CountAsync();

            // Sửa lại công thức Skip phân trang chính xác
            var taiKhoans = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;

            return View(taiKhoans);
        }

        // ==========================================
        // GET: TaiKhoan/Details/5
        // ==========================================
        public async Task<IActionResult> Details(int? mataikhoan)
        {
            if (mataikhoan == null)
                return NotFound();

            var taikhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(m => m.MaTaiKhoan == mataikhoan);

            if (taikhoan == null)
                return NotFound();

            return View(taikhoan);
        }

        // ==========================================
        // GET: TaiKhoan/Create
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            // Kiểm tra Role Admin không phân biệt chữ hoa/thường (Admin/ADMIN)
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
        // POST: TaiKhoan/Create (Dùng TaiKhoanKhachHang ViewModel)
        // ==========================================
        [HttpPost]
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
                    return RedirectToAction("Indexx");
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
        // GET: TaiKhoan/Edit/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? mataikhoan)
        {
            if (mataikhoan == null)
                return NotFound();

            var taikhoan = await _context.TaiKhoans.FindAsync(mataikhoan);
            if (taikhoan == null)
                return NotFound();

            return View(taikhoan);
        }

        // ==========================================
        // POST: TaiKhoan/Edit/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int? mataikhoan,
            [Bind("MaTaiKhoan,Username,Password,Role,TrangThai")] TaiKhoan taikhoan)
        {
            if (mataikhoan != taikhoan.MaTaiKhoan)
                return NotFound();

            if (!ModelState.IsValid)
                return View(taikhoan);

            try
            {
                _context.Update(taikhoan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaiKhoanExists(taikhoan.MaTaiKhoan))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: TaiKhoan/Delete/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? mataikhoan)
        {
            if (mataikhoan == null)
                return NotFound();

            var taikhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(m => m.MaTaiKhoan == mataikhoan);

            if (taikhoan == null)
                return NotFound();

            return View(taikhoan);
        }

        // ==========================================
        // POST: TaiKhoan/Delete/5
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? mataikhoan)
        {
            var taikhoan = await _context.TaiKhoans.FindAsync(mataikhoan);
            if (taikhoan != null)
            {
                try
                {
                    _context.TaiKhoans.Remove(taikhoan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = "Không thể xóa: " + ex.InnerException?.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return RedirectToAction(nameof(Index));
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
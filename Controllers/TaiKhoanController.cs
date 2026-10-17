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
     // Cho phép người dùng chưa đăng nhập tự đăng ký
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
        // ==========================================
        // GET: TaiKhoan/Index?keyword=&role=&trangThai=&page=1
        // ==========================================
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Index(
            string? keyword,
            string? role,
            string? trangThai,
            int? page)
        {
            const int pageSize = 5;
            int pageNumber = (page ?? 1) < 1 ? 1 : (page ?? 1);

            // 1. Khởi tạo query
            var query = _context.TaiKhoans.AsQueryable();

            // 2. Tìm kiếm theo Username (hoặc thêm các trường khác nếu muốn)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(t => t.Username.Contains(keyword));
            }

            // 3. Lọc theo Role
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(t => t.Role == role);
            }

            // 4. Lọc theo Trạng thái
            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(t => t.TrangThai == trangThai);
            }

            // 5. Sắp xếp cố định để phân trang ổn định
            query = query.OrderBy(t => t.MaTaiKhoan);

            // 6. Đếm tổng & phân trang
            int totalCount = await query.CountAsync();
            var taiKhoans = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // 7. Truyền dữ liệu qua ViewBag
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;

            // Giữ lại giá trị filter để View hiển thị đúng
            ViewBag.Keyword = keyword;
            ViewBag.Role = role;
            ViewBag.TrangThai = trangThai;

            // Nạp danh sách cho dropdown lọc
            ViewBag.RoleList = new List<SelectListItem>
    {
        new SelectListItem { Value = "Admin",     Text = "Admin" },
        new SelectListItem { Value = "NhanVien",  Text = "Nhân viên" },
        new SelectListItem { Value = "USER", Text = "Khách hàng" }
    };

            ViewBag.TrangThaiList = new List<SelectListItem>
    {
        new SelectListItem { Value = "HoatDong", Text = "Hoạt động" },
        new SelectListItem { Value = "Khoa",     Text = "Khóa" }
    };

            return View(taiKhoans);
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
        // ==========================================
        // GET: TaiKhoan/Create
        // ==========================================
        [AllowAnonymous]
        [HttpGet]
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
                LoadRoleListIfAdmin(isAdmin);
                return View(model);
            }

            // 3. Phân quyền: không phải Admin thì ép "KhachHang"
            string assignedRole = isAdmin ? (model.Role ?? "KhachHang") : "KhachHang";

            // 4. Mở Transaction lưu 2 bảng atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // BƯỚC A: Tạo TaiKhoan
                var taiKhoan = new TaiKhoan
                {
                    Username = model.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password, 10),
                    Role = assignedRole,
                    TrangThai = "HoatDong"
                };

                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync(); // sinh MaTaiKhoan

                // BƯỚC B: Lưu vào bảng tương ứng với Role
                if (assignedRole == "NhanVien")
                {
                    var nhanVien = new NhanVien
                    {
                        HoTen = model.HoTen,
                        Sdt = model.Sdt,
                        Email = model.Email,
                        MaTaiKhoan = taiKhoan.MaTaiKhoan
                        // Nếu entity NhanVien có Cccd thì thêm: Cccd = model.Cccd,
                    };
                    _context.NhanViens.Add(nhanVien);
                }
                else // "KhachHang" (hoặc mặc định)
                {
                    var khachHang = new KhachHang
                    {
                        HoTen = model.HoTen,
                        Sdt = model.Sdt,
                        Email = model.Email,
                        Cccd = model.Cccd,
                        MaTaiKhoan = taiKhoan.MaTaiKhoan
                    };
                    _context.KhachHangs.Add(khachHang);
                }

                await _context.SaveChangesAsync();

                // BƯỚC C: Commit
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
                LoadRoleListIfAdmin(isAdmin);
                return View(model);
            }
        }

        // ==========================================
        // Helper: nạp lại RoleList khi trả về View
        // ==========================================
        private void LoadRoleListIfAdmin(bool isAdmin)
        {
            if (!isAdmin) return;

            ViewBag.RoleList = new List<SelectListItem>
    {
        new SelectListItem { Value = "KhachHang", Text = "Khách hàng" },
        new SelectListItem { Value = "NhanVien", Text = "Nhân viên" }
    };
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
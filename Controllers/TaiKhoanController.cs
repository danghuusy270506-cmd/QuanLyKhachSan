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
            string assignedRole;

            if (!isAdmin)
            {
                // Khách tự đăng ký chỉ được tạo tài khoản khách hàng
                assignedRole = "KhachHang";
            }
            else
            {
                assignedRole = (model.Role ?? string.Empty).Trim();

                // Chỉ cho phép Admin tạo KhachHang hoặc NhanVien
                if (assignedRole != "KhachHang" &&
                    assignedRole != "NhanVien")
                {
                    ModelState.AddModelError(
                        nameof(model.Role),
                        "Vai trò tài khoản không hợp lệ.");

                    ViewBag.RoleList = new List<SelectListItem>
                    {
                        new SelectListItem
                        {
                            Value = "KhachHang",
                            Text = "Khách hàng"
                        },
                        new SelectListItem
                        {
                            Value = "NhanVien",
                            Text = "Nhân viên"
                        }
                    };

                    return View(model);
                }
            }

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

                // BƯỚC B: Tạo hồ sơ đúng theo Role đã chọn
                if (assignedRole == "KhachHang")
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
                else if (assignedRole == "NhanVien")
                {
                    var nhanVien = new NhanVien
                    {
                        HoTen = model.HoTen,
                        Sdt = model.Sdt,
                        Email = model.Email,
                        Cccd = model.Cccd,
                        MaTaiKhoan = taiKhoan.MaTaiKhoan
                    };

                    _context.NhanViens.Add(nhanVien);
                }

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
            // Chỉ chấp nhận các Role hợp lệ
            string? roleInput = taikhoan.Role?.Trim();

            string? normalizedRole = new[]
            {
                "Admin",
                "NhanVien",
                "KhachHang"
            }.FirstOrDefault(r =>
                string.Equals(
                    r,
                    roleInput,
                    StringComparison.OrdinalIgnoreCase));

            if (normalizedRole == null)
            {
                ModelState.AddModelError(
                    nameof(taikhoan.Role),
                    "Vai trò tài khoản không hợp lệ.");
            }
            else
            {
                // Chuẩn hóa Role theo đúng giá trị dùng trong ứng dụng
                taikhoan.Role = normalizedRole;
            }
            // Chuẩn hóa tên đăng nhập
            taikhoan.Username = taikhoan.Username?.Trim();

            // Không cho phép tên đăng nhập trống
            if (string.IsNullOrWhiteSpace(taikhoan.Username))
            {
                ModelState.AddModelError(
                    nameof(taikhoan.Username),
                    "Tên đăng nhập không được để trống.");
            }
            else
            {
                // Kiểm tra tên đăng nhập đã được tài khoản khác sử dụng chưa
                bool usernameExists = await _context.TaiKhoans
                    .AnyAsync(t =>
                        t.Username == taikhoan.Username &&
                        t.MaTaiKhoan != taikhoan.MaTaiKhoan);

                if (usernameExists)
                {
                    ModelState.AddModelError(
                        nameof(taikhoan.Username),
                        "Tên đăng nhập này đã được tài khoản khác sử dụng.");
                }
            }
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
                using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    // Lấy tài khoản cần sửa ở trạng thái tracking
                    var accountToUpdate = await _context.TaiKhoans
                        .FirstOrDefaultAsync(
                            t => t.MaTaiKhoan == mataikhoan);

                    if (accountToUpdate == null)
                        return NotFound();

                    // Tìm hồ sơ hiện tại của tài khoản
                    var khachHang = await _context.KhachHangs
                        .FirstOrDefaultAsync(
                            k => k.MaTaiKhoan == mataikhoan);

                    var nhanVien = await _context.NhanViens
                        .FirstOrDefaultAsync(
                            n => n.MaTaiKhoan == mataikhoan);

                    // Một tài khoản không được gắn đồng thời hai hồ sơ
                    if (khachHang != null && nhanVien != null)
                    {
                        await transaction.RollbackAsync();

                        ModelState.AddModelError(
                            nameof(taikhoan.Role),
                            "Tài khoản đang liên kết với cả hồ sơ khách hàng " +
                            "và nhân viên. Cần kiểm tra dữ liệu trước khi sửa.");

                        if (Request.Headers["X-Requested-With"]
                            == "XMLHttpRequest")
                            return PartialView("_EditPartial", taikhoan);

                        return View(taikhoan);
                    }

                    bool roleChanged = !string.Equals(
                        existingAccount.Role,
                        taikhoan.Role,
                        StringComparison.OrdinalIgnoreCase);

                    // Chỉ chuyển đổi hồ sơ khi Role thực sự thay đổi
                    if (roleChanged)
                    {
                        // Chuyển sang vai trò KhachHang
                        if (taikhoan.Role == "KhachHang")
                        {
                            // Không có hồ sơ nguồn để chuyển đổi
                            if (khachHang == null && nhanVien == null)
                            {
                                await transaction.RollbackAsync();

                                ModelState.AddModelError(
                                    nameof(taikhoan.Role),
                                    "Tài khoản chưa có hồ sơ cá nhân. " +
                                    "Không thể tự động chuyển vai trò.");

                                if (Request.Headers["X-Requested-With"]
                                    == "XMLHttpRequest")
                                    return PartialView("_EditPartial", taikhoan);

                                return View(taikhoan);
                            }

                            // Chuyển hồ sơ nhân viên sang khách hàng
                            if (khachHang == null && nhanVien != null)
                            {
                                bool coLichSu = await _context.PhieuDatPhongs
                                    .AnyAsync(p =>
                                        p.MaNhanVien == nhanVien.MaNhanVien);

                                if (coLichSu)
                                {
                                    await transaction.RollbackAsync();

                                    ModelState.AddModelError(
                                        nameof(taikhoan.Role),
                                        "Nhân viên đã có lịch sử xử lý phiếu đặt. " +
                                        "Không thể chuyển sang vai trò khách hàng.");

                                    if (Request.Headers["X-Requested-With"]
                                        == "XMLHttpRequest")
                                        return PartialView("_EditPartial", taikhoan);

                                    return View(taikhoan);
                                }

                                _context.NhanViens.Remove(nhanVien);

                                _context.KhachHangs.Add(new KhachHang
                                {
                                    HoTen = nhanVien.HoTen,
                                    Cccd = nhanVien.Cccd,
                                    Sdt = nhanVien.Sdt,
                                    Email = nhanVien.Email,
                                    MaTaiKhoan = nhanVien.MaTaiKhoan
                                });
                            }
                        }
                        // Chuyển sang vai trò NhanVien hoặc Admin
                        else
                        {
                            // Không có hồ sơ nguồn để chuyển đổi
                            if (khachHang == null && nhanVien == null)
                            {
                                await transaction.RollbackAsync();

                                ModelState.AddModelError(
                                    nameof(taikhoan.Role),
                                    "Tài khoản chưa có hồ sơ nhân viên hoặc " +
                                    "khách hàng để chuyển đổi.");

                                if (Request.Headers["X-Requested-With"]
                                    == "XMLHttpRequest")
                                    return PartialView("_EditPartial", taikhoan);

                                return View(taikhoan);
                            }

                            // Chuyển hồ sơ khách hàng sang nhân viên
                            if (nhanVien == null && khachHang != null)
                            {
                                bool coLichSu = await _context.PhieuDatPhongs
                                    .AnyAsync(p =>
                                        p.MaKhachHang == khachHang.MaKhachHang);

                                if (coLichSu)
                                {
                                    await transaction.RollbackAsync();

                                    ModelState.AddModelError(
                                        nameof(taikhoan.Role),
                                        "Khách hàng đã có lịch sử đặt phòng. " +
                                        "Không thể chuyển sang vai trò nhân viên.");

                                    if (Request.Headers["X-Requested-With"]
                                        == "XMLHttpRequest")
                                        return PartialView("_EditPartial", taikhoan);

                                    return View(taikhoan);
                                }

                                _context.KhachHangs.Remove(khachHang);

                                _context.NhanViens.Add(new NhanVien
                                {
                                    HoTen = khachHang.HoTen,
                                    Cccd = khachHang.Cccd,
                                    Sdt = khachHang.Sdt,
                                    Email = khachHang.Email,
                                    MaTaiKhoan = khachHang.MaTaiKhoan
                                });
                            }
                        }
                    }

                    // Cập nhật tài khoản hiện có
                    accountToUpdate.Username = taikhoan.Username;
                    accountToUpdate.Password = taikhoan.Password;
                    accountToUpdate.Role = taikhoan.Role;
                    accountToUpdate.TrangThai = taikhoan.TrangThai;

                    // Lưu thay đổi tài khoản và hồ sơ trong cùng transaction
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (Request.Headers["X-Requested-With"]
                        == "XMLHttpRequest")
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Cập nhật tài khoản thành công!"
                        });
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "Không thể cập nhật tài khoản. " +
                        "Hãy kiểm tra dữ liệu liên quan rồi thử lại.");

                    if (Request.Headers["X-Requested-With"]
                        == "XMLHttpRequest")
                        return PartialView("_EditPartial", taikhoan);

                    return View(taikhoan);
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
            bool isAjax =
                Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!mataikhoan.HasValue)
            {
                if (isAjax)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy tài khoản cần xóa."
                    });
                }

                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.MaTaiKhoan == mataikhoan.Value);

            if (taiKhoan == null)
            {
                if (isAjax)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tài khoản không tồn tại."
                    });
                }

                return NotFound();
            }

            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.MaTaiKhoan == taiKhoan.MaTaiKhoan);

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(n => n.MaTaiKhoan == taiKhoan.MaTaiKhoan);

            // Kiểm tra lịch sử đặt phòng của khách hàng
            bool khachHangCoLichSu = khachHang != null &&
                await _context.PhieuDatPhongs.AnyAsync(
                    p => p.MaKhachHang == khachHang.MaKhachHang);

            // Kiểm tra lịch sử nghiệp vụ của nhân viên
            bool nhanVienCoLichSu = nhanVien != null &&
                await _context.PhieuDatPhongs.AnyAsync(
                    p => p.MaNhanVien == nhanVien.MaNhanVien);

            // Nếu có lịch sử nghiệp vụ thì không xóa dữ liệu
            if (khachHangCoLichSu || nhanVienCoLichSu)
            {
                taiKhoan.TrangThai = "Khoa";

                await _context.SaveChangesAsync();

                const string message =
                    "Tài khoản có lịch sử nghiệp vụ nên không thể xóa. " +
                    "Hệ thống đã khóa tài khoản và giữ nguyên hồ sơ liên quan.";

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

            // Chỉ xóa khi không có lịch sử nghiệp vụ
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                if (khachHang != null)
                {
                    _context.KhachHangs.Remove(khachHang);
                }

                if (nhanVien != null)
                {
                    _context.NhanViens.Remove(nhanVien);
                }

                _context.TaiKhoans.Remove(taiKhoan);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        deleted = true,
                        message = "Đã xóa tài khoản và hồ sơ liên quan."
                    });
                }

                TempData["SuccessMessage"] =
                    "Đã xóa tài khoản và hồ sơ liên quan.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                const string message =
                    "Không thể xóa tài khoản. Hãy kiểm tra các dữ liệu liên quan.";

                if (isAjax)
                {
                    return Json(new
                    {
                        success = false,
                        message
                    });
                }

                TempData["Error"] = message;
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
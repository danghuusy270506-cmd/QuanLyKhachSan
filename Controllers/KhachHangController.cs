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
    public class KhachHangController : Controller
    {
        private readonly QLKhachSanContext _context;

        public KhachHangController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. Danh sách khách hàng: TÌM KIẾM THEO TÊN + PHÂN TRANG (Admin & NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Index(string? tuKhoa, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;

            var query = _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .AsNoTracking()
                .AsQueryable();

            // ===== BỔ SUNG: Tìm kiếm theo Họ tên (và SĐT, Email, CCCD) =====
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string searchKey = tuKhoa.Trim().ToLower();
                query = query.Where(k =>
                    k.HoTen.ToLower().Contains(searchKey) ||
                    (k.Sdt != null && k.Sdt.Contains(searchKey)) ||
                    (k.Email != null && k.Email.ToLower().Contains(searchKey)) ||
                    (k.Cccd != null && k.Cccd.Contains(searchKey)));
            }

            // Tính tổng số lượng bản ghi & tổng số trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages == 0) totalPages = 1;
            page = Math.Min(page, totalPages);

            // Lấy dữ liệu phân trang
            var khachHangs = await query
                .OrderByDescending(k => k.MaKhachHang)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu sang View để hiển thị bộ lọc & thanh phân trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TuKhoa = tuKhoa;

            // Phản hồi AJAX cho giao diện làm tươi danh sách
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_KhachHangPartial", khachHangs);
            }

            return View(khachHangs);
        }

        // 2. Chi tiết 1 khách hàng
        [HttpGet]
        public async Task<IActionResult> Details(int maKhachHang)
        {
            if (maKhachHang <= 0)
                return BadRequest("ID không hợp lệ!");

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            // Kiểm tra quyền riêng tư của Khách hàng
            if (User.IsInRole("KhachHang") && !IsOwner(khachHang))
            {
                return Forbid();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DetailsPartial", khachHang);
            }

            return View(khachHang);
        }

        // 3. Form thêm khách hàng mới (Admin & NhanVien)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public IActionResult Create()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", new KhachHang());
            }
            return View();
        }

        // 4. Xử lý Thêm khách hàng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create([Bind("HoTen,Cccd,Sdt,Email")] KhachHang khachHang)
        {
            if (string.IsNullOrWhiteSpace(khachHang.Email) && string.IsNullOrWhiteSpace(khachHang.Sdt))
            {
                ModelState.AddModelError("", "Vui lòng nhập ít nhất Số điện thoại hoặc Email để liên lạc.");
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Sdt) &&
                await _context.KhachHangs.AnyAsync(k => k.Sdt == khachHang.Sdt))
            {
                ModelState.AddModelError("Sdt", "Số điện thoại này đã tồn tại trên hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Email) &&
                await _context.KhachHangs.AnyAsync(k => k.Email == khachHang.Email))
            {
                ModelState.AddModelError("Email", "Email này đã tồn tại trên hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(khachHang.Cccd) &&
                await _context.KhachHangs.AnyAsync(k => k.Cccd == khachHang.Cccd))
            {
                ModelState.AddModelError("Cccd", "Số CCCD này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string username = !string.IsNullOrWhiteSpace(khachHang.Sdt)
                        ? khachHang.Sdt.Trim()
                        : khachHang.Email?.Trim() ?? $"kh_{DateTime.Now.Ticks}";

                    var taiKhoan = new TaiKhoan
                    {
                        Username = username,
                        Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                        Role = "KhachHang",
                        TrangThai = "HoatDong"
                    };

                    _context.TaiKhoans.Add(taiKhoan);
                    await _context.SaveChangesAsync();

                    khachHang.MaTaiKhoan = taiKhoan.MaTaiKhoan;
                    _context.KhachHangs.Add(khachHang);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Nếu gọi bằng Ajax thì trả về JSON thành công
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = $"Thêm khách hàng '{khachHang.HoTen}' thành công!" });
                    }

                    TempData["SuccessMessage"] = $"Thêm thông tin khách hàng '{khachHang.HoTen}' thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình lưu dữ liệu. Vui lòng thử lại.");
                }
            }

            // Nếu Ajax submit lỗi, trả lại PartialView chứa lỗi validation
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", khachHang);
            }

            return View(khachHang);
        }

        // 5. Hiển thị trang Cập nhật
        [HttpGet]
        public async Task<IActionResult> Edit(int maKhachHang)
        {
            var khachHang = await _context.KhachHangs
        .Include(k => k.TaiKhoan)
        .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            if (User.IsInRole("KhachHang") && !IsOwner(khachHang))
            {
                return Forbid();
            }

            // Nếu gọi bằng Ajax thì trả về PartialView chứa form sửa
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", khachHang);
            }

            return View(khachHang);
        }

        // 6. Xử lý Cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int maKhachHang,KhachHang model,string? newPassword)
        {
            var khachHang = await _context.KhachHangs
        .Include(k => k.TaiKhoan)
        .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            if (User.IsInRole("KhachHang") && !IsOwner(khachHang))
            {
                return Forbid();
            }

            // Kiểm tra trùng lặp thông tin
            if (!string.IsNullOrWhiteSpace(model.Email) &&
                await _context.KhachHangs.AnyAsync(k => k.Email == model.Email && k.MaKhachHang != maKhachHang))
            {
                ModelState.AddModelError("Email", "Email này đã được dùng cho khách hàng khác.");
            }

            if (!string.IsNullOrWhiteSpace(model.Sdt) &&
                await _context.KhachHangs.AnyAsync(k => k.Sdt == model.Sdt && k.MaKhachHang != maKhachHang))
            {
                ModelState.AddModelError("Sdt", "Số điện thoại này đã được dùng cho khách hàng khác.");
            }

            if (!string.IsNullOrWhiteSpace(model.Cccd) &&
                await _context.KhachHangs.AnyAsync(k => k.Cccd == model.Cccd && k.MaKhachHang != maKhachHang))
            {
                ModelState.AddModelError("Cccd", "Số CCCD này đã được dùng cho khách hàng khác.");
            }

            if (ModelState.IsValid)
            {
                khachHang.HoTen = model.HoTen;
                khachHang.Cccd = model.Cccd;
                khachHang.Sdt = model.Sdt;
                khachHang.Email = model.Email;

                if (!string.IsNullOrWhiteSpace(newPassword) && khachHang.TaiKhoan != null)
                {
                    khachHang.TaiKhoan.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                await _context.SaveChangesAsync();

                // Nếu là Ajax request, trả về JSON thành công
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = $"Cập nhật khách hàng '{khachHang.HoTen}' thành công!" });
                }

                return RedirectToAction(nameof(Details), new { maKhachHang = khachHang.MaKhachHang });
            }

            // Nếu lỗi Validation và gọi qua Ajax, trả về lại PartialView kèm lỗi
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", khachHang);
            }

            return View(khachHang);
        }

        // 7. Hiển thị Form Xóa dạng Modal (AJAX)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Delete(int maKhachHang)
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            // Nếu là gọi AJAX thì trả về PartialView hiển thị trong modal
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DeletePartial", khachHang);
            }

            return View(khachHang);
        }

        // 8. Xử lý Xóa khách hàng (AJAX & POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]

        public async Task<IActionResult> Delete(int maKhachHang, IFormCollection collection) // Thêm tham số hoặc giữ nguyên tùy ý để phân biệt chữ ký nếu cần, ở đây dùng chung tên nhưng xử lý theo header AJAX
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.PhieuDatPhongs)
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Không tìm thấy khách hàng!" });

                return NotFound("Không tìm thấy khách hàng có ID này!");
            }

            // Kiểm tra ràng buộc dữ liệu (ví dụ: đã có phiếu đặt phòng hay chưa)
            if (khachHang.PhieuDatPhongs.Any())
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Không thể xóa khách hàng đã có phiếu đặt phòng." });

                TempData["Error"] = "Không thể xóa khách hàng đã có phiếu đặt phòng.";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = khachHang.TaiKhoan;

            // Xóa khách hàng
            _context.KhachHangs.Remove(khachHang);
            await _context.SaveChangesAsync();

            // Xóa tài khoản liên quan nếu có
            if (taiKhoan != null)
            {
                _context.TaiKhoans.Remove(taiKhoan);
                await _context.SaveChangesAsync();
            }

            // Phản hồi kết quả về cho AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = $"Xóa khách hàng '{khachHang.HoTen}' thành công!" });
            }

            return RedirectToAction(nameof(Index));
        }
        private bool IsOwner(KhachHang khachHang)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return khachHang.MaTaiKhoan == currentUserId;
            }
            return false;
        }
    }
}
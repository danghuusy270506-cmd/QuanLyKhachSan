using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly QLKhachSanContext _context;

        public KhachHangController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. Danh sách khách hàng
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var khachHangs = await _context.KhachHangs
                .Include(k => k.TaiKhoan)   // ✅ Thêm để hiển thị username
                .ToListAsync();
            return View(khachHangs);
        }

        // 2. Chi tiết 1 khách hàng
        [HttpGet]
        public async Task<IActionResult> Details(int maKhachHang)
        {
            if (maKhachHang <= 0)
                return BadRequest("ID không hợp lệ!");

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)   // ✅ Thêm
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            return View(khachHang);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ==========================================
        // 2. XỬ LÝ NHÂN VIÊN NHẬP THÔNG TIN KHÁCH HÀNG (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("HoTen,Cccd,Sdt,Email")] KhachHang khachHang,
            bool taoTaiKhoanWeb = false) // Tùy chọn Nhân viên có muốn tạo tài khoản Web cho khách hay không
        {
            // 1. Kiểm tra điều kiện: Bắt buộc nhập ít nhất SĐT hoặc Email để liên hệ
            if (string.IsNullOrWhiteSpace(khachHang.Email) && string.IsNullOrWhiteSpace(khachHang.Sdt))
            {
                ModelState.AddModelError("", "Vui lòng nhập ít nhất Số điện thoại hoặc Email để liên lạc.");
            }

            // 2. Kiểm tra trùng lặp thông tin Khách hàng dưới Database
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

            // 3. Nếu dữ liệu nhập hợp lệ
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Tự động sinh Username từ SĐT hoặc Email
                    string username = !string.IsNullOrWhiteSpace(khachHang.Sdt)
                        ? khachHang.Sdt.Trim()
                        : khachHang.Email?.Trim() ?? $"kh_{DateTime.Now.Ticks}";

                    // Xử lý tạo Tài khoản đăng nhập (Nếu chọn tạo hoặc nếu DB bắt buộc MaTaiKhoan)
                    var taiKhoan = new TaiKhoan
                    {
                        Username = username,
                        Password = BCrypt.Net.BCrypt.HashPassword("123456"), // Mật khẩu mặc định cho khách
                        Role = "KhachHang",
                        TrangThai = "HoatDong"
                    };

                    _context.TaiKhoans.Add(taiKhoan);
                    await _context.SaveChangesAsync(); // Lưu để sinh MaTaiKhoan

                    // Gán MaTaiKhoan cho Khách hàng
                    khachHang.MaTaiKhoan = taiKhoan.MaTaiKhoan;
                    _context.KhachHangs.Add(khachHang);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Thêm thông tin khách hàng '{khachHang.HoTen}' thành công!";

                    // Sau khi Nhân viên tạo xong Khách hàng -> Chuyển thẳng sang trang Tạo Phiếu Đặt Phòng
                    return RedirectToAction("Create", "PhieuDatPhong", new { maKhachHang = khachHang.MaKhachHang });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình lưu dữ liệu. Vui lòng thử lại.");
                }
            }

            return View(khachHang);
        }
    

        // 5. Hiển thị trang Cập nhật
        [HttpGet]
        public async Task<IActionResult> Edit(int maKhachHang)
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)   // ✅ Thêm
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            return View(khachHang);
        }

        // 6. Xử lý Cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int maKhachHang,
            KhachHang model,
            string? newPassword)
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            // ===== Validate trùng Email/Sdt/Cccd (trừ chính nó) =====
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

            if (!ModelState.IsValid)
                return View(khachHang);

            // ===== Cập nhật thông tin cơ bản =====
            khachHang.HoTen = model.HoTen;
            khachHang.Cccd = model.Cccd;
            khachHang.Sdt = model.Sdt;
            khachHang.Email = model.Email;

            // ===== Cập nhật mật khẩu nếu có =====
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (khachHang.TaiKhoan != null)
                    khachHang.TaiKhoan.Password = newPassword;   // ⚠️ Nên hash
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { maKhachHang = khachHang.MaKhachHang });
        }

        // 7. Xử lý Xóa khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int maKhachHang)
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)                // ✅ Cần Include để lấy TaiKhoan
                .Include(k => k.PhieuDatPhongs)          // ✅ Kiểm tra có phiếu đặt chưa
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return NotFound("Không tìm thấy khách hàng có ID này!");

            // ✅ Chặn xóa nếu có phiếu đặt phòng
            if (khachHang.PhieuDatPhongs.Any())
            {
                TempData["Error"] = "Không thể xóa khách hàng đã có phiếu đặt phòng.";
                return RedirectToAction(nameof(Index));
            }

            // Lưu lại TaiKhoan để xóa sau
            var taiKhoan = khachHang.TaiKhoan;

            // Xóa KhachHang trước (nó chứa FK)
            _context.KhachHangs.Remove(khachHang);
            await _context.SaveChangesAsync();

            // Xóa TaiKhoan sau (nếu có)
            if (taiKhoan != null)
            {
                _context.TaiKhoans.Remove(taiKhoan);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
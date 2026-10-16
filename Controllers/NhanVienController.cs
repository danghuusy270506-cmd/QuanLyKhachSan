//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using QuanLyKhachSan.Models.Data;
//using QuanLyKhachSan.Models.Entities;
//using System.Threading.Tasks;

//namespace QuanLyKhachSan.Controllers
//{
//    public class NhanVienController : Controller
//    {
//        private readonly QLKhachSanContext _context;

//        public NhanVienController(QLKhachSanContext context)
//        {
//            _context = context;
//        }

   
//        [HttpGet]
//        public async Task<IActionResult> Index()
//        {
//            var danhSachNhanVien = await _context.NhanViens.ToListAsync();
//            return View(danhSachNhanVien);
//        }

//        // 2. Chi tiết 1 nhân viên
//        [HttpGet]
//        public async Task<IActionResult> Details(int maNhanVien)
//        {
//            if (maNhanVien <= 0)
//            {
//                return BadRequest("ID nhân viên không hợp lệ!");
//            }

//            var nhanVien = await _context.NhanViens
//                .Include(nv => nv.PhieuDatPhongs) // Nạp danh sách phiếu đặt phòng nếu muốn hiển thị
//                .FirstOrDefaultAsync(nv => nv.MaNhanVien == maNhanVien);

//            if (nhanVien == null)
//            {
//                return NotFound("Không tìm thấy nhân viên có ID này!");
//            }

//            return View(nhanVien);
//        }

//        // 3. Hiển thị trang Thêm mới nhân viên
//        [HttpGet]
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // 4. Xử lý Thêm mới nhân viên
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(NhanVien nhanVien)
//        {
//            if (ModelState.IsValid)
//            {
//                // Mặc định trạng thái nếu người dùng để trống
//                if (string.IsNullOrWhiteSpace(nhanVien.TrangThai))
//                {
//                    nhanVien.TrangThai = "Đang làm việc";
//                }

//                // Băm mật khẩu bằng BCrypt trước khi lưu
//                if (!string.IsNullOrEmpty(nhanVien.Password))
//                {
//                    nhanVien.Password = BCrypt.Net.BCrypt.HashPassword(nhanVien.Password);
//                }

//                _context.NhanViens.Add(nhanVien);
//                await _context.SaveChangesAsync();

//                return RedirectToAction(nameof(Details), new { maNhanVien = nhanVien.MaNhanVien });
//            }

//            return View(nhanVien);
//        }

//        // 5. Hiển thị trang Cập nhật thông tin nhân viên
//        [HttpGet]
//        public async Task<IActionResult> Edit(int maNhanVien)
//        {
//            if (maNhanVien <= 0)
//            {
//                return BadRequest("ID nhân viên không hợp lệ!");
//            }

//            var nhanVien = await _context.NhanViens.FindAsync(maNhanVien);
//            if (nhanVien == null)
//            {
//                return NotFound("Không tìm thấy nhân viên có ID này!");
//            }

//            return View(nhanVien);
//        }

//        // 6. Xử lý Cập nhật thông tin nhân viên
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int maNhanVien, NhanVien model, string? newPassword)
//        {
//            if (maNhanVien != model.MaNhanVien)
//            {
//                return BadRequest("ID nhân viên không trùng khớp!");
//            }

//            var nhanVien = await _context.NhanViens.FindAsync(maNhanVien);
//            if (nhanVien == null)
//            {
//                return NotFound("Không tìm thấy nhân viên có ID này!");
//            }

//            // Cập nhật các thông tin cơ bản
//            nhanVien.HoTen = model.HoTen;
//            nhanVien.Cccd = model.Cccd;
//            nhanVien.Sdt = model.Sdt;
//            nhanVien.Email = model.Email;
//            nhanVien.Username = model.Username;
//            nhanVien.TrangThai = model.TrangThai;

//            // Chỉ đổi mật khẩu nếu quản trị viên/nhân viên nhập mật khẩu mới
//            if (!string.IsNullOrWhiteSpace(newPassword))
//            {
//                nhanVien.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
//            }

//            await _context.SaveChangesAsync();

//            return RedirectToAction(nameof(Details), new { maNhanVien = nhanVien.MaNhanVien });
//        }

//        // 7. Xử lý Xóa nhân viên
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(int maNhanVien)
//        {
//            var nhanVien = await _context.NhanViens.FindAsync(maNhanVien);
//            if (nhanVien == null)
//            {
//                return NotFound("Không tìm thấy nhân viên có ID này!");
//            }

//            _context.NhanViens.Remove(nhanVien);
//            await _context.SaveChangesAsync();

//            return RedirectToAction(nameof(Index));
//        }
//    }
//}
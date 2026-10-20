using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    [Authorize]
    public class PhieuDatPhongController : Controller
    {
        private readonly QLKhachSanContext _context;

        public PhieuDatPhongController(QLKhachSanContext context)
        {
            _context = context;
        }


        // 1. DS PHIẾU ĐẶT PHÒNG
        [HttpGet]
        public async Task<IActionResult> Index(int? page, string maPhieu, string khachHang, string trangThai, int? maLoaiPhong, DateTime? ngayDen, DateTime? ngayDi)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;
            var query = _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.LoaiPhong)
                .Include(p => p.NhanVien)
                .Include(p => p.ChiTietXepPhongs)
                    .ThenInclude(ct => ct.Phong)
                .AsQueryable();
            // Giới hạn dữ liệu theo vai trò
            if (User.IsInRole("KhachHang"))
            {
                var userIdStr =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdStr, out int maTaiKhoan))
                    return Forbid();

                var maKhachHang = await _context.KhachHangs
                    .Where(k => k.MaTaiKhoan == maTaiKhoan)
                    .Select(k => (int?)k.MaKhachHang)
                    .FirstOrDefaultAsync();

                if (!maKhachHang.HasValue)
                    return Forbid();

                query = query.Where(
                    p => p.MaKhachHang == maKhachHang.Value);
            }
            else if (!User.IsInRole("NhanVien")
                     && !User.IsInRole("Admin")
                     && !User.IsInRole("ADMIN"))
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(maPhieu))
            {
                string ma = maPhieu.Replace("#", "").Trim();
                if (int.TryParse(ma, out int maPhieuDatPhong))
                {
                    query = query.Where(p => p.MaPhieu == maPhieuDatPhong);
                }
            }
            if (!string.IsNullOrEmpty(khachHang))
            {
                string keyWord = khachHang.Trim().ToLower();
                query = query.Where(p => p.KhachHang != null && p.KhachHang.HoTen.ToLower().Contains(keyWord));
            }
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }
            if (maLoaiPhong.HasValue && maLoaiPhong.Value > 0)
            {
                query = query.Where(p => p.MaLoaiPhong == maLoaiPhong.Value);
            }
            if (ngayDen.HasValue)
            {
                query = query.Where(p => p.NgayDen.Date >= ngayDen.Value.Date);
            }
            if (ngayDi.HasValue)
            {
                query = query.Where(p => p.NgayDi.Date <= ngayDi.Value.Date);
            }

            query = query.OrderByDescending(p => p.MaPhieu);
            int soLuongPhieu = await query.CountAsync();
            int tongSoTrang = (int)Math.Ceiling(soLuongPhieu / (double)pageSize);
            var dsPhieu = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = tongSoTrang;

            ViewBag.maPhieu = maPhieu;
            ViewBag.khachHang = khachHang;
            ViewBag.trangThai = trangThai;
            ViewBag.maLoaiPhong = maLoaiPhong;
            ViewBag.ngayDen = ngayDen?.ToString("yyyy-MM-dd");
            ViewBag.ngayDi = ngayDi?.ToString("yyyy-MM-dd");
            ViewData["ListLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", maLoaiPhong);

            ViewData["ListTrangThai"] = new SelectList(new List<string>
{
                "Chờ xác nhận",
                "Đã xác nhận",
                "Đang ở",
                "Chờ thanh toán",
                "Hoàn thành",
                "Đã hủy"
            }, trangThai);


            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PhieuDatPhongPartial", dsPhieu);
            }
            return View(dsPhieu);
        }

        // 2. CHI TIẾT PHIẾU ĐẶT PHÒNG
        [HttpGet]
        public async Task<IActionResult> Details(int? maphieu)
        {
            if (maphieu == null)
                return NotFound();

            var query = _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.LoaiPhong)
                .Include(p => p.NhanVien)
                .AsQueryable();

            // Khách hàng chỉ được xem phiếu của chính mình
            if (User.IsInRole("KhachHang"))
            {
                var userIdStr =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdStr, out int maTaiKhoan))
                    return Forbid();

                var maKhachHang = await _context.KhachHangs
                    .Where(k => k.MaTaiKhoan == maTaiKhoan)
                    .Select(k => (int?)k.MaKhachHang)
                    .FirstOrDefaultAsync();

                if (!maKhachHang.HasValue)
                    return Forbid();

                query = query.Where(
                    p => p.MaKhachHang == maKhachHang.Value);
            }
            else if (!User.IsInRole("NhanVien")
                     && !User.IsInRole("Admin")
                     && !User.IsInRole("ADMIN"))
            {
                return Forbid();
            }

            var phieudatphong = await query
                .FirstOrDefaultAsync(p => p.MaPhieu == maphieu);

            if (phieudatphong == null)
                return NotFound();

            return PartialView("_DetailsPartial", phieudatphong);
        }

        // 3. GIAO DIỆN ĐẶT PHÒNG (GET: Create)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int maTaiKhoan))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Content("<div class='modal-header bg-danger text-white'><h5 class='modal-title'>Thông báo</h5><button type='button' class='btn-close btn-close-white' data-bs-dismiss='modal'></button></div><div class='modal-body p-4 text-center'><h6>Vui lòng đăng nhập tài khoản để thực hiện đặt phòng!</h6></div>", "text/html");
                }
                return RedirectToAction("Authentication", "DangNhap");
            }

            bool isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("ADMIN") || User.IsInRole("NhanVien");

            int? selectedKhachHangId = null;
            if (!isAdminOrStaff)
            {
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);
                if (khachHang == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Content("<div class='modal-header bg-danger text-white'><h5 class='modal-title'>Thông báo</h5><button type='button' class='btn-close btn-close-white' data-bs-dismiss='modal'></button></div><div class='modal-body p-4 text-center'><h6>Không tìm thấy thông tin khách hàng tương ứng!</h6></div>", "text/html");
                    }
                    return RedirectToAction("Authentication", "DangNhap");
                }
                selectedKhachHangId = khachHang.MaKhachHang;
                ViewBag.DanhSachKhachHang = await _context.KhachHangs.Where(k => k.MaKhachHang == khachHang.MaKhachHang).ToListAsync();
            }
            else
            {
                ViewBag.DanhSachKhachHang = await _context.KhachHangs.ToListAsync();
            }

            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong");

            var phieuMoi = new PhieuDatPhong();
            if (selectedKhachHangId.HasValue)
            {
                phieuMoi.MaKhachHang = selectedKhachHangId.Value;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", phieuMoi);
            }
            return View(phieuMoi);
        }

        // 4. XỬ LÝ ĐẶT PHÒNG (POST: Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NgayDen,NgayDi,TienCoc,SoNguoi,MaLoaiPhong,MaKhachHang")] PhieuDatPhong phieudatphong)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int maTaiKhoan))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
                }
                return RedirectToAction("Authentication", "DangNhap");
            }

            bool isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("ADMIN") || User.IsInRole("NhanVien");

            if (!isAdminOrStaff)
            {
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTaiKhoan == maTaiKhoan);
                if (khachHang == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng." });
                    }
                    return RedirectToAction("Authentication", "DangNhap");
                }
                phieudatphong.MaKhachHang = khachHang.MaKhachHang;
                // Khách hàng không được tự quyết định tiền đặt cọc
                phieudatphong.TienCoc = 0m;

                // Bỏ lỗi validation của giá trị TienCoc do khách gửi lên
                ModelState.Remove(nameof(PhieuDatPhong.TienCoc));
            }

            phieudatphong.NgayDat = DateTime.Now;
            phieudatphong.TrangThai = "Chờ xác nhận";

            // Validate logic ngày đến & ngày đi

            // Kiểm tra ngày nhận phòng không được ở trong quá khứ
            if (phieudatphong.NgayDen.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(phieudatphong.NgayDen),
                    "Ngày nhận phòng không được ở trong quá khứ.");
            }

            // Ngày trả phòng phải sau ngày nhận phòng
            if (phieudatphong.NgayDi.Date <= phieudatphong.NgayDen.Date)
            {
                ModelState.AddModelError(
                    nameof(phieudatphong.NgayDi),
                    "Ngày trả phòng phải sau ngày nhận phòng.");
            }


            if (phieudatphong.MaKhachHang <= 0)
            {
                ModelState.AddModelError("MaKhachHang", "Vui lòng chọn khách hàng.");
            }

            // Kiểm tra loại phòng được chọn có tồn tại không
            var loaiPhong = await _context.LoaiPhongs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    lp => lp.MaLoaiPhong == phieudatphong.MaLoaiPhong);

            if (loaiPhong == null)
            {
                ModelState.AddModelError(
                    nameof(phieudatphong.MaLoaiPhong),
                    "Vui lòng chọn loại phòng hợp lệ.");
            }
            else if (phieudatphong.SoNguoi > loaiPhong.SucChua)
            {
                ModelState.AddModelError(
                    nameof(phieudatphong.SoNguoi),
                    $"Loại phòng này chỉ dành cho tối đa " +
                    $"{loaiPhong.SucChua} người.");
            }

            // Kiểm tra phòng khả dụng theo từng ngày lưu trú
            if (ModelState.IsValid && loaiPhong != null)
            {
                var ngayDen = phieudatphong.NgayDen.Date;
                var ngayDi = phieudatphong.NgayDi.Date;

                // Các trạng thái phòng được tính vào nguồn phòng
                int soPhongKhaDung = await _context.Phongs
                    .CountAsync(p =>
                        p.MaLoaiPhong == loaiPhong.MaLoaiPhong &&
                        (p.TrangThai == "Trống" ||
                         p.TrangThai == "Đang ở" ||
                         p.TrangThai == "Đang sử dụng"));

                if (soPhongKhaDung == 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Loại phòng hiện không có phòng khả dụng.");
                }
                else
                {
                    // Các phiếu đang giữ nhu cầu đặt phòng
                    string[] trangThaiDangDat =
                    {
                        "Chờ xác nhận",
                        "Đã xác nhận",
                        "Đang ở"
                    };

                    // Lấy các phiếu cùng loại phòng có ngày giao nhau
                    var cacPhieuTrungLich =
                        await _context.PhieuDatPhongs
                            .AsNoTracking()
                            .Where(p =>
                                p.MaLoaiPhong == loaiPhong.MaLoaiPhong &&
                                trangThaiDangDat.Contains(p.TrangThai) &&
                                p.NgayDen.Date < ngayDi &&
                                p.NgayDi.Date > ngayDen)
                            .Select(p => new
                            {
                                p.NgayDen,
                                p.NgayDi
                            })
                            .ToListAsync();

                    int soPhieuTrungNhieuNhat = 0;

                    // Tính số phiếu đồng thời trong từng đêm
                    for (var ngay = ngayDen;
                         ngay < ngayDi;
                         ngay = ngay.AddDays(1))
                    {
                        int soPhieuTrongDem = cacPhieuTrungLich.Count(p =>
                            p.NgayDen.Date <= ngay &&
                            p.NgayDi.Date > ngay);

                        if (soPhieuTrongDem > soPhieuTrungNhieuNhat)
                        {
                            soPhieuTrungNhieuNhat = soPhieuTrongDem;
                        }
                    }

                    // Phải còn ít nhất một phòng cho yêu cầu mới
                    if (soPhieuTrungNhieuNhat >= soPhongKhaDung)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Loại phòng đã hết khả năng phục vụ " +
                            "trong khoảng ngày đã chọn. " +
                            "Vui lòng chọn ngày khác.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(phieudatphong);
                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true });
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống khi lưu phiếu đặt phòng: " + ex.Message);
                }
            }

            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieudatphong.MaLoaiPhong);
            if (!isAdminOrStaff)
            {
                ViewBag.DanhSachKhachHang = await _context.KhachHangs.Where(k => k.MaKhachHang == phieudatphong.MaKhachHang).ToListAsync();
            }
            else
            {
                ViewBag.DanhSachKhachHang = await _context.KhachHangs.ToListAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", phieudatphong);
            }
            return View(phieudatphong);
        }

        // 5. GIAO DIỆN SỬA PHIẾU (GET: Edit)
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(int? maphieu)
        {
            if (maphieu == null) return NotFound();

            var phieuDatPhong = await _context.PhieuDatPhongs.FindAsync(maphieu);
            if (phieuDatPhong == null) return NotFound();

            ViewData["MaKhachHang"] = new SelectList(_context.KhachHangs, "MaKhachHang", "HoTen", phieuDatPhong.MaKhachHang);
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieuDatPhong.MaLoaiPhong);
            return PartialView("_EditPartial", phieuDatPhong);
        }

        // 6. XỬ LÝ SỬA PHIẾU (POST: Edit)
        [Authorize(Roles = "Admin,NhanVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int maphieu, [Bind("MaPhieu,NgayDat,NgayDen,NgayDi,TienCoc,SoNguoi,TrangThai,MaKhachHang,MaNhanVien,MaLoaiPhong")] PhieuDatPhong phieudatphong)
        {
            if (maphieu != phieudatphong.MaPhieu) return NotFound();

            // Khi sửa phiếu, ngày trả phải sau ngày nhận
            if (phieudatphong.NgayDi.Date <= phieudatphong.NgayDen.Date)
            {
                ModelState.AddModelError(
                    nameof(phieudatphong.NgayDi),
                    "Ngày trả phòng phải sau ngày nhận phòng.");
            }

            // Kiểm tra loại phòng được chọn khi sửa phiếu
            var loaiPhongChon = await _context.LoaiPhongs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    lp => lp.MaLoaiPhong == phieudatphong.MaLoaiPhong);

            if (loaiPhongChon == null)
            {
                ModelState.AddModelError(
                    nameof(phieudatphong.MaLoaiPhong),
                    "Vui lòng chọn loại phòng hợp lệ.");
            }
            else if (phieudatphong.SoNguoi > loaiPhongChon.SucChua)
            {
                ModelState.AddModelError(
                    nameof(phieudatphong.SoNguoi),
                    $"Loại phòng này chỉ dành cho tối đa " +
                    $"{loaiPhongChon.SucChua} người.");
            }

            // Kiểm tra lịch trùng khi sửa phiếu
            string[] trangThaiDangDat =
            {
                "Chờ xác nhận",
                "Đã xác nhận",
                "Đang ở"
            };

            if (ModelState.IsValid &&
                loaiPhongChon != null &&
                trangThaiDangDat.Contains(phieudatphong.TrangThai))
            {
                var ngayDen = phieudatphong.NgayDen.Date;
                var ngayDi = phieudatphong.NgayDi.Date;

                // Tổng số phòng được tính vào nguồn phòng khả dụng
                int soPhongKhaDung = await _context.Phongs
                    .CountAsync(p =>
                        p.MaLoaiPhong == loaiPhongChon.MaLoaiPhong &&
                        (p.TrangThai == "Trống" ||
                         p.TrangThai == "Đang ở" ||
                         p.TrangThai == "Đang sử dụng"));

                if (soPhongKhaDung == 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Loại phòng hiện không có phòng khả dụng.");
                }
                else
                {
                    // Loại trừ chính phiếu đang sửa
                    var cacPhieuTrungLich =
                        await _context.PhieuDatPhongs
                            .AsNoTracking()
                            .Where(p =>
                                p.MaPhieu != phieudatphong.MaPhieu &&
                                p.MaLoaiPhong == loaiPhongChon.MaLoaiPhong &&
                                trangThaiDangDat.Contains(p.TrangThai) &&
                                p.NgayDen.Date < ngayDi &&
                                p.NgayDi.Date > ngayDen)
                            .Select(p => new
                            {
                                p.NgayDen,
                                p.NgayDi
                            })
                            .ToListAsync();

                    int soPhieuTrungNhieuNhat = 0;

                    // Tìm đêm có nhiều phiếu trùng lịch nhất
                    for (var ngay = ngayDen;
                         ngay < ngayDi;
                         ngay = ngay.AddDays(1))
                    {
                        int soPhieuTrongDem = cacPhieuTrungLich.Count(p =>
                            p.NgayDen.Date <= ngay &&
                            p.NgayDi.Date > ngay);

                        if (soPhieuTrongDem > soPhieuTrungNhieuNhat)
                        {
                            soPhieuTrungNhieuNhat = soPhieuTrongDem;
                        }
                    }

                    if (soPhieuTrungNhieuNhat >= soPhongKhaDung)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Loại phòng đã hết khả năng phục vụ " +
                            "trong khoảng ngày đã chọn. " +
                            "Vui lòng chọn ngày khác.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPhieu = await _context.PhieuDatPhongs
                        .Include(p => p.KhachHang)
                        .Include(p => p.LoaiPhong)
                        .Include(p => p.NhanVien)
                        .FirstOrDefaultAsync(p => p.MaPhieu == maphieu);

                    if (existingPhieu == null) return NotFound();

                    existingPhieu.MaKhachHang = phieudatphong.MaKhachHang;
                    existingPhieu.MaLoaiPhong = phieudatphong.MaLoaiPhong;
                    existingPhieu.NgayDen = phieudatphong.NgayDen;
                    existingPhieu.NgayDi = phieudatphong.NgayDi;
                    existingPhieu.SoNguoi = phieudatphong.SoNguoi;
                    existingPhieu.TienCoc = phieudatphong.TienCoc;
                    existingPhieu.TrangThai = phieudatphong.TrangThai;

                    _context.Update(existingPhieu);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhieuDatPhongExists(phieudatphong.MaPhieu)) return NotFound();
                    else throw;
                }
            }

            ViewData["MaKhachHang"] = new SelectList(_context.KhachHangs, "MaKhachHang", "HoTen", phieudatphong.MaKhachHang);
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phieudatphong.MaLoaiPhong);
            return PartialView("_EditPartial", phieudatphong);
        }

        // 7. GIAO DIỆN XÓA PHIẾU (GET: Delete)
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Delete(int? maphieu)
        {
            if (maphieu == null) return NotFound();

            var phieudatphong = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(m => m.MaPhieu == maphieu);

            if (phieudatphong == null) return NotFound();

            return PartialView("_DeletePartial", phieudatphong);
        }

        // 8. XỬ LÝ XÓA PHIẾU (POST: Delete)
        [Authorize(Roles = "Admin,NhanVien")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int maphieu)
        {
            var phieudatphong = await _context.PhieuDatPhongs.FindAsync(maphieu);
            if (phieudatphong != null)
            {
                _context.PhieuDatPhongs.Remove(phieudatphong);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng để xóa." });
        }

        private bool PhieuDatPhongExists(int maphieu)
        {
            return _context.PhieuDatPhongs.Any(e => e.MaPhieu == maphieu);
        }



        // Xác nhận phiếu đặt phòng và kiểm tra sức chứa
        [Authorize(Roles = "Admin,NhanVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhan(int maphieu)
        {
            var phieu = await _context.PhieuDatPhongs
                .FirstOrDefaultAsync(p => p.MaPhieu == maphieu);

            if (phieu == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy phiếu đặt phòng."
                });
            }

            // Chỉ xác nhận phiếu đang chờ xác nhận
            if (phieu.TrangThai != "Chờ xác nhận")
            {
                return Json(new
                {
                    success = false,
                    message = "Phiếu không còn ở trạng thái chờ xác nhận."
                });
            }

            // Tổng số phòng thuộc loại này, loại trừ phòng bảo trì
            int tongSoPhong = await _context.Phongs
                .CountAsync(p =>
                    p.MaLoaiPhong == phieu.MaLoaiPhong &&
                    (p.TrangThai == "Trống" ||
                     p.TrangThai == "Đang ở" ||
                     p.TrangThai == "Đang sử dụng"));

            if (tongSoPhong == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Loại phòng này hiện không có phòng khả dụng."
                });
            }

            // Đếm các phiếu đã xác nhận hoặc đang ở
            // có thời gian lưu trú chồng lấn với phiếu hiện tại
            string[] trangThaiDaGiuPhong =
            {
                "Đã xác nhận",
                "Đang ở"
            };

            int soPhieuDaGiuPhong =
                await _context.PhieuDatPhongs
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.MaPhieu != phieu.MaPhieu &&
                        p.MaLoaiPhong == phieu.MaLoaiPhong &&
                        trangThaiDaGiuPhong.Contains(p.TrangThai) &&
                        p.NgayDen.Date < phieu.NgayDi.Date &&
                        p.NgayDi.Date > phieu.NgayDen.Date);

            // Nếu toàn bộ sức chứa đã được các phiếu khác sử dụng,
            // không cho xác nhận thêm
            if (soPhieuDaGiuPhong >= tongSoPhong)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Không thể xác nhận phiếu vì loại phòng đã hết " +
                        "khả năng phục vụ trong khoảng ngày đã chọn."
                });
            }

            // Cập nhật trạng thái khi tất cả kiểm tra đều hợp lệ
            phieu.TrangThai = "Đã xác nhận";

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Xác nhận phiếu đặt phòng thành công."
            });
        }


    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using QuanLyKhachSan.Models.ViewModels;
using System.Security.Claims;

namespace QuanLyKhachSan.Controllers
{
    // Controller đặt phòng dành riêng cho khách hàng
    [Authorize(Roles = "KhachHang")]
    public class ClientDatPhongController : Controller
    {
        private readonly QLKhachSanContext _context;

        public ClientDatPhongController(
            QLKhachSanContext context)
        {
            _context = context;
        }

        // GET: Hiển thị form đặt phòng
        [HttpGet]
        public async Task<IActionResult> Create(int? maLoaiPhong)
        {
            // Kiểm tra mã loại phòng
            if (!maLoaiPhong.HasValue)
                return NotFound();

            // Lấy mã tài khoản từ JWT
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // Xác nhận tài khoản có hồ sơ khách hàng
            var khachHang = await _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    k => k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return Forbid();

            // Lấy thông tin loại phòng từ database
            var loaiPhong = await _context.LoaiPhongs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    lp => lp.MaLoaiPhong == maLoaiPhong.Value);

            if (loaiPhong == null)
                return NotFound();

            // Chuẩn bị dữ liệu ban đầu cho form
            var model = new ClientDatPhongViewModel
            {
                MaLoaiPhong = loaiPhong.MaLoaiPhong,
                NgayDen = DateTime.Today.AddDays(1),
                NgayDi = DateTime.Today.AddDays(2),
                SoNguoi = 1
            };

            // Thông tin để View hiển thị tên và giá phòng
            ViewBag.LoaiPhong = loaiPhong;

            return View(model);
        }

        // POST: Xử lý yêu cầu đặt phòng của khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ClientDatPhongViewModel model)
        {
            // 1. Xác định tài khoản đang đăng nhập
            var userIdStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int maTaiKhoan))
                return Forbid();

            // 2. Lấy hồ sơ khách hàng từ tài khoản đăng nhập
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(
                    k => k.MaTaiKhoan == maTaiKhoan);

            if (khachHang == null)
                return Forbid();

            // 3. Kiểm tra loại phòng được yêu cầu
            var loaiPhong = await _context.LoaiPhongs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    lp => lp.MaLoaiPhong == model.MaLoaiPhong);

            if (loaiPhong == null)
                return NotFound();

            // Giữ thông tin loại phòng để hiển thị lại
            // nếu dữ liệu người dùng nhập không hợp lệ.
            ViewBag.LoaiPhong = loaiPhong;

            // 4. Kiểm tra số khách không vượt sức chứa
            if (ModelState.IsValid &&
                model.SoNguoi > loaiPhong.SucChua)
            {
                ModelState.AddModelError(
                    nameof(model.SoNguoi),
                    $"Loại phòng này chỉ dành cho tối đa " +
                    $"{loaiPhong.SucChua} người.");
            }


            // Kiểm tra khả năng phục vụ theo từng đêm lưu trú
            if (ModelState.IsValid)
            {
                var ngayDen = model.NgayDen!.Value.Date;
                var ngayDi = model.NgayDi!.Value.Date;

                // Đếm phòng được tính vào nguồn phòng khả dụng
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

                    // Tìm đêm có số phiếu trùng lịch nhiều nhất
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


            // 5. Nếu dữ liệu sai, quay lại form
            if (!ModelState.IsValid)
                return View(model);

            // 6. Tạo phiếu đặt phòng
            var phieuDatPhong = new PhieuDatPhong
            {
                NgayDat = DateTime.Now,
                NgayDen = model.NgayDen!.Value.Date,
                NgayDi = model.NgayDi!.Value.Date,
                SoNguoi = model.SoNguoi,

                // Khách chưa phải trả tiền cọc tại bước này
                TienCoc = 0m,

                // Nhân viên sẽ xử lý yêu cầu sau
                TrangThai = "Chờ xác nhận",

                // Lấy từ hồ sơ khách hàng đã xác thực
                MaKhachHang = khachHang.MaKhachHang,

                // Chưa phân công nhân viên
                MaNhanVien = null,

                // Dùng mã loại phòng đã kiểm tra trong database
                MaLoaiPhong = loaiPhong.MaLoaiPhong
            };

            // 7. Lưu xuống database
            _context.PhieuDatPhongs.Add(phieuDatPhong);
            await _context.SaveChangesAsync();

            // Ghi thông báo để dùng ở bước giao diện tiếp theo
            TempData["SuccessMessage"] =
                $"Yêu cầu đặt phòng đã được gửi. " +
                $"Mã phiếu: {phieuDatPhong.MaPhieu}.";

            // Trở về danh sách loại phòng
            return RedirectToAction(
                "Index", "ClientLoaiPhong");
        }

    }
}

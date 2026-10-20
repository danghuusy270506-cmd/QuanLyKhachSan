using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using QuanLyKhachSan.Models.ViewModels;

namespace QuanLyKhachSan.Controllers
{
    // Yêu cầu đăng nhập và chỉ cho phép Admin hoặc NhanVien truy cập
    [Authorize(Roles = "Admin,NhanVien")]
    public class ChiTietXepPhongController : Controller
    {
        private readonly QLKhachSanContext context;

        public ChiTietXepPhongController(QLKhachSanContext context)
        {
            this.context = context;
        }

        // Danh sách phiếu Đã xác nhận, chờ khách đến check-in
        public async Task<IActionResult> Index()
        {
            var danhSach = await context.PhieuDatPhongs
                           .Include(p => p.KhachHang)
                           .Include(p => p.LoaiPhong)
                           .Where(p => p.TrangThai == "Đã xác nhận")
                           .OrderBy(p => p.NgayDen)
                           .ToListAsync();

            return View(danhSach);
        }

        // Hiển thị thông tin phiếu và danh sách phòng phù hợp
        [HttpGet]
        public async Task<IActionResult> XepPhong(int maPhieu)
        {
            var phieu = await context.PhieuDatPhongs
                        .Include(p => p.KhachHang)
                        .Include(p => p.LoaiPhong)
                        .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null) return NotFound("Không tìm thấy phiếu đặt phòng.");

            if (phieu.TrangThai != "Đã xác nhận")
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest("Chỉ được xếp phòng cho phiếu đã xác nhận.");

                TempData["Error"] = "Chỉ được xếp phòng cho phiếu đã xác nhận.";
                return RedirectToAction(nameof(Index));
            }

            bool daXepPhong = await context.ChiTietXepPhongs.AnyAsync(ct => ct.MaPhieu == maPhieu);
            if (daXepPhong)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest("Phiếu này đã có phòng được xếp.");

                TempData["Error"] = "Phiếu này đã có phòng được xếp.";
                return RedirectToAction(nameof(Index));
            }

            var phongPhuHop = await LayPhongTrong(phieu);
            var selectListItems = phongPhuHop.Select(p => new SelectListItem
            {
                Value = p.MaPhong.ToString(),
                Text = $"Phòng {p.SoPhong} — Tầng {p.Tang} — Trống"
            }).ToList();
            ViewBag.DanhSachPhong = new SelectList(selectListItems, "Value", "Text");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_XepPhongPartial", phieu);
            }

            return View(phieu);
        }


        // POST: Xếp phòng cho phiếu, chưa thực hiện check-in
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XepPhong(
            int maPhieu,
            int maPhong)
        {
            // Lấy phiếu và loại phòng
            var phieu = await context.PhieuDatPhongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.ChiTietXepPhongs)
                .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy phiếu đặt phòng."
                });
            }

            // Chỉ được xếp phòng cho phiếu đã xác nhận
            if (phieu.TrangThai != "Đã xác nhận")
            {
                return Json(new
                {
                    success = false,
                    message = "Chỉ được xếp phòng cho phiếu đã xác nhận."
                });
            }

            // Mỗi phiếu hiện chỉ được xếp một phòng
            if (phieu.ChiTietXepPhongs.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Phiếu này đã được xếp phòng."
                });
            }

            if (phieu.LoaiPhong == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin loại phòng."
                });
            }

            // Kiểm tra lại phòng trong database:
            // phải đúng loại và còn khả dụng theo lịch lưu trú
            var phongPhuHop = await LayPhongTrong(phieu);

            var phong = phongPhuHop.FirstOrDefault(
                p => p.MaPhong == maPhong);

            if (phong == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Phòng đã được sử dụng, không còn khả dụng " +
                        "hoặc không đúng loại phòng đã đặt."
                });
            }

            int donGia = (int)phieu.LoaiPhong.GiaPhong;

            if (donGia <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Giá loại phòng không hợp lệ."
                });
            }

            // Chỉ lưu phòng được phân, chưa ghi nhận check-in
            var chiTiet = new ChiTietXepPhong
            {
                MaPhieu = maPhieu,
                MaPhong = phong.MaPhong,
                DonGia = donGia,
                NgayNhanPhongThucTe = null,
                NgayTra = null
            };

            context.ChiTietXepPhongs.Add(chiTiet);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Không thể lưu thông tin xếp phòng. " +
                        "Vui lòng tải lại dữ liệu và thử lại."
                });
            }

            // Không thay đổi trạng thái phiếu hoặc phòng tại đây.
            // Check-in sẽ do action CheckIn xử lý riêng.
            return Json(new
            {
                success = true,
                message =
                    $"Đã xếp phòng {phong.SoPhong} cho phiếu " +
                    $"#{phieu.MaPhieu}. Khách chưa được check-in.",
                maPhieu = maPhieu,
                maPhong = phong.MaPhong,
                soPhong = phong.SoPhong
            });
        }


        // POST: Check-in các phòng đã được xếp cho phiếu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int maPhieu)
        {
            var phieu = await context.PhieuDatPhongs
                .Include(p => p.ChiTietXepPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy phiếu đặt phòng."
                });
            }

            // Chỉ check-in phiếu đã được xác nhận
            if (phieu.TrangThai != "Đã xác nhận")
            {
                return Json(new
                {
                    success = false,
                    message = "Phiếu không ở trạng thái có thể check-in."
                });
            }

            var danhSachChiTiet = phieu.ChiTietXepPhongs.ToList();

            // Phải xếp phòng trước khi check-in
            if (!danhSachChiTiet.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng xếp phòng trước khi check-in."
                });
            }

            // Kiểm tra các phòng được xếp đúng loại và đang trống
            if (danhSachChiTiet.Any(ct =>
                ct.Phong == null ||
                ct.Phong.MaLoaiPhong != phieu.MaLoaiPhong ||
                ct.Phong.TrangThai != "Trống"))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Có phòng không còn khả dụng hoặc không đúng loại phòng."
                });
            }

            // Không ghi nhận check-in lần thứ hai
            if (danhSachChiTiet.Any(ct =>
                ct.NgayNhanPhongThucTe.HasValue ||
                ct.NgayTra.HasValue))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Thông tin phòng đã có thời điểm nhận hoặc trả phòng."
                });
            }

            var thoiGianNhan = DateTime.Now;

            foreach (var chiTiet in danhSachChiTiet)
            {
                chiTiet.NgayNhanPhongThucTe = thoiGianNhan;
                chiTiet.Phong!.TrangThai = "Đang ở";
            }

            phieu.TrangThai = "Đang ở";

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Không thể ghi nhận check-in. Vui lòng kiểm tra lại dữ liệu."
                });
            }

            return Json(new
            {
                success = true,
                message = "Check-in thành công.",
                maPhieu = maPhieu
            });
        }

        // GET: Mở form ghi nhận dịch vụ cho khách đang lưu trú
        [HttpGet]
        public async Task<IActionResult> GhiNhanDichVu(int maPhieu)
        {
            var phieu = await context.PhieuDatPhongs
                .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null)
                return NotFound();

            // Chỉ ghi nhận dịch vụ khi khách đang lưu trú
            if (phieu.TrangThai != "Đang ở")
            {
                TempData["Error"] =
                    "Chỉ có thể ghi nhận dịch vụ khi khách đang lưu trú.";

                return RedirectToAction("Index", "PhieuDatPhong");
            }

            // Kiểm tra còn phòng đang được khách sử dụng
            bool coPhongDangO = await context.ChiTietXepPhongs
                .AnyAsync(ct =>
                    ct.MaPhieu == maPhieu &&
                    ct.NgayNhanPhongThucTe != null &&
                    ct.NgayTra == null);

            if (!coPhongDangO)
            {
                TempData["Error"] =
                    "Phiếu này không có phòng nào đang lưu trú.";

                return RedirectToAction("Index", "PhieuDatPhong");
            }

            var model = new GhiNhanDichVuViewModel
            {
                MaPhieu = maPhieu,
                SoLuong = 1
            };

            await NapDuLieuFormDichVuAsync(model);

            return View(model);
        }

        // Chuẩn bị danh sách phòng và dịch vụ cho form
        private async Task NapDuLieuFormDichVuAsync(
            GhiNhanDichVuViewModel model)
        {
            // Chỉ cho chọn phòng thuộc phiếu này
            // và khách chưa check-out phòng đó
            var chiTietPhong = await context.ChiTietXepPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                .Where(ct =>
                    ct.MaPhieu == model.MaPhieu &&
                    ct.NgayNhanPhongThucTe != null &&
                    ct.NgayTra == null)
                .OrderBy(ct => ct.Phong!.SoPhong)
                .ToListAsync();

            var danhSachPhong = chiTietPhong.Select(ct =>
                new SelectListItem
                {
                    Value = ct.MaChiTiet.ToString(),
                    Text = ct.Phong == null
                        ? $"Mã chi tiết {ct.MaChiTiet}"
                        : $"Phòng {ct.Phong.SoPhong} - Tầng {ct.Phong.Tang}"
                }).ToList();

            ViewBag.DanhSachPhong = new SelectList(
                danhSachPhong,
                "Value",
                "Text",
                model.MaChiTietXepPhong > 0
                    ? model.MaChiTietXepPhong.ToString()
                    : null);

            var danhSachDichVu = await context.DichVus
                .AsNoTracking()
                .OrderBy(dv => dv.TenDichVu)
                .ToListAsync();

            ViewBag.DanhSachDichVu = new SelectList(
                danhSachDichVu,
                "MaDichVu",
                "TenDichVu",
                model.MaDichVu);

            ViewBag.MaPhieu = model.MaPhieu;
        }

        // Lấy các phòng đúng loại và không bị trùng lịch
        private async Task<List<Phong>> LayPhongTrong(PhieuDatPhong phieu)
        {
            // Các trạng thái không còn giữ phòng
            string[] trangThaiKetThuc =
            {
                "Hoàn thành",
                "Đã hủy",
                "Đã hủy đặt phòng"
            };

            // Những phòng đã được xếp cho phiếu khác có lịch chồng lấn
            var phongBiTrungLich = await context.ChiTietXepPhongs
                .Where(ct =>
                    ct.MaPhieu != phieu.MaPhieu &&
                    !trangThaiKetThuc.Contains(
                        ct.PhieuDatPhong!.TrangThai
                    ) &&
                    ct.PhieuDatPhong.NgayDen < phieu.NgayDi &&
                    ct.PhieuDatPhong.NgayDi > phieu.NgayDen
                )
                .Select(ct => ct.MaPhong)
                .Distinct()
                .ToListAsync();

            // Chỉ lấy phòng đúng loại và trạng thái Trống
            var danhSachPhong = await context.Phongs
                .Where(p => p.MaLoaiPhong == phieu.MaLoaiPhong && p.TrangThai == "Trống" && !phongBiTrungLich.Contains(p.MaPhong))
                .OrderBy(p => p.SoPhong)
                .ToListAsync();

            return danhSachPhong;
        }

        // Check-out khách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int maPhieu)
        {
            // Kiểm tra request có phải AJAX không
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // Tìm phiếu đặt phòng và các phòng đã xếp

            var phieu = await context.PhieuDatPhongs
                .Include(p => p.ChiTietXepPhongs)
                    .ThenInclude(ct => ct.Phong)
                .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });
                return NotFound();
            }

            // Chỉ cho check-out phiếu đang ở
            if (phieu.TrangThai != "Đang ở")
            {
                if (isAjax)
                    return Json(new { success = false, message = "Phiếu đặt phòng không ở trạng thái đang ở." });

                TempData["ThongBao"] = "Phiếu đặt phòng không ở trạng thái đang ở.";
                return RedirectToAction("Index", "PhieuDatPhong");
            }

            var chiTietPhong = phieu.ChiTietXepPhongs.ToList();

            if (!chiTietPhong.Any())
            {
                if (isAjax)
                    return Json(new { success = false, message = "Phiếu đặt phòng chưa được xếp phòng." });

                TempData["ThongBao"] = "Phiếu đặt phòng chưa được xếp phòng.";
                return RedirectToAction("Index", "PhieuDatPhong");
            }

            // Lấy các phòng chưa check-out
            var phongChuaTra = chiTietPhong.Where(ct => ct.NgayTra == null).ToList();
            if (!phongChuaTra.Any())
            {
                // Đã check-out hết → chuyển sang tạo hóa đơn
                if (isAjax)
                    return Json(new { success = true, message = "Các phòng đã được check-out trước đó.", maPhieu = maPhieu });

                TempData["ThongBao"] = "Các phòng của phiếu này đã được check-out.";
                return RedirectToAction("Create", "HoaDon", new { maPhieu = maPhieu });
            }

            // Kiểm tra các phòng đã có thời điểm check-in
            if (phongChuaTra.Any(ct => ct.NgayNhanPhongThucTe == null))
            {
                if (isAjax)
                    return Json(new { success = false, message = "Có phòng chưa ghi nhận thời gian check-in." });

                TempData["ThongBao"] = "Có phòng chưa ghi nhận thời gian check-in.";
                return RedirectToAction("Index", "PhieuDatPhong");
            }

            // Ghi nhận thời gian trả phòng thực tế
            DateTime thoiGianTra = DateTime.Now;
            foreach (var ct in phongChuaTra)
            {
                // Ghi nhận thời điểm khách trả phòng
                ct.NgayTra = thoiGianTra;
                // Chuyển phòng sang trạng thái chờ dọn
                if (ct.Phong != null)
                {
                    ct.Phong.TrangThai = "Đang dọn dẹp";
                }
            }
            phieu.TrangThai = "Chờ thanh toán";
            await context.SaveChangesAsync();

            // Trả về JSON cho AJAX (JS sẽ tiếp tục load form Create hóa đơn)
            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    message = "Check-out thành công.",
                    maPhieu = maPhieu
                });
            }

            // Fallback cho request thường
            TempData["ThongBao"] = "Check-out thành công. Vui lòng kiểm tra hóa đơn.";
            return RedirectToAction("Create", "HoaDon", new { maPhieu = maPhieu });
        }
    }
}
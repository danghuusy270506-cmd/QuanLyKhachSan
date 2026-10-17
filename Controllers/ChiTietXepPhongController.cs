
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;

namespace QuanLyKhachSan.Controllers
{
    public class ChiTietXepPhongController : Controller
    {
        private readonly QLKhachSanContext context;

        public ChiTietXepPhongController(QLKhachSanContext context)
        {
            this.context = context;
        }

        //Danh sách phiếu Đã xác nhận, chờ khách đến check-in
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

        // Xác nhận xếp phòng và check-in
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XepPhong(int maPhieu, int maPhong)
        {
            // Lấy phiếu đặt phòng và loại phòng
            var phieu = await context.PhieuDatPhongs
                        .Include(p => p.LoaiPhong)
                        .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null)
                return Json(new { success = false, message = "Không tìm thấy phiếu đặt phòng." });

            if (phieu.TrangThai != "Đã xác nhận")
                return Json(new { success = false, message = "Phiếu không ở trạng thái có thể check-in." });

            bool daXepPhong = await context.ChiTietXepPhongs.AnyAsync(ct => ct.MaPhieu == maPhieu);
            if (daXepPhong)
                return Json(new { success = false, message = "Phiếu này đã được xếp phòng." });

            var phongPhuHop = await LayPhongTrong(phieu);
            var phong = phongPhuHop.FirstOrDefault(p => p.MaPhong == maPhong);
            if (phong == null)
                return Json(new { success = false, message = "Phòng không còn khả dụng hoặc không đúng loại phòng." });

            int donGia = (int)phieu.LoaiPhong.GiaPhong;
            if (donGia <= 0)
                return Json(new { success = false, message = "Giá loại phòng không hợp lệ." });

            var chiTiet = new ChiTietXepPhong
            {
                MaPhieu = maPhieu,
                MaPhong = maPhong,
                DonGia = donGia,
                NgayNhanPhongThucTe = DateTime.Now,
                NgayTra = null
            };

            phieu.TrangThai = "Đang ở";
            phong.TrangThai = "Đang ở";

            context.ChiTietXepPhongs.Add(chiTiet);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, message = "Không thể lưu thông tin xếp phòng. Vui lòng kiểm tra lại dữ liệu." });
            }

            return Json(new
            {
                success = true,
                message = $"Check-in thành công cho phòng {phong.SoPhong}!",
                maPhieu = maPhieu
            });
        }


        //Lấy các phòng đúng loại và không bị trùng lịch
        private async Task<System.Collections.Generic.List<Phong>>LayPhongTrong(PhieuDatPhong phieu)
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
                .Where(p =>p.MaLoaiPhong == phieu.MaLoaiPhong && p.TrangThai == "Trống" && !phongBiTrungLich.Contains(p.MaPhong))
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
                ct.NgayTra = thoiGianTra;
            }

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

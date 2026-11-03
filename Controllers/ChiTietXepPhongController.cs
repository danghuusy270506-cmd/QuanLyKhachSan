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

            var danhSachPhong = danhSachChiTiet.Where(ct => ct.Phong != null).Select(ct => new
            {
                maChiTietXepPhong = ct.MaChiTiet,
                soPhong = ct.Phong!.SoPhong
            })
            .ToList();

            return Json(new
            {
                success = true,
                message = "Check-in thành công.",
                maPhieu = maPhieu,
                danhSachPhong = danhSachPhong
            });
        }

        // GET: Mở form ghi nhận dịch vụ cho khách đang lưu trú
        [HttpGet]
        public async Task<IActionResult> GhiNhanDichVu(
    int maPhieu,
    int? maChiTietXepPhong)
        {
            var phieu = await context.PhieuDatPhongs
                .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null)
                return NotFound();

            if (phieu.TrangThai != "Đang ở")
            {
                TempData["Error"] =
                    "Chỉ có thể ghi nhận dịch vụ khi khách đang lưu trú.";

                return RedirectToAction("Index", "PhieuDatPhong");
            }

            // Lấy các phòng đang được khách sử dụng
            var danhSachMaChiTiet = await context.ChiTietXepPhongs
                .AsNoTracking()
                .Where(ct =>
                    ct.MaPhieu == maPhieu &&
                    ct.NgayNhanPhongThucTe != null &&
                    ct.NgayTra == null)
                .Select(ct => ct.MaChiTiet)
                .ToListAsync();

            if (!danhSachMaChiTiet.Any())
            {
                TempData["Error"] =
                    "Phiếu này không có phòng nào đang lưu trú.";

                return RedirectToAction("Index", "PhieuDatPhong");
            }

            // Nếu phòng được truyền lên không thuộc phiếu
            // hoặc không còn đang lưu trú thì từ chối
            if (maChiTietXepPhong.HasValue &&
                !danhSachMaChiTiet.Contains(maChiTietXepPhong.Value))
            {
                return BadRequest("Phòng không hợp lệ hoặc đã check-out.");
            }

            // Nếu phiếu chỉ có một phòng đang ở,
            // tự động chọn phòng đó
            if (!maChiTietXepPhong.HasValue &&
                danhSachMaChiTiet.Count == 1)
            {
                maChiTietXepPhong = danhSachMaChiTiet[0];
            }

            var model = new GhiNhanDichVuViewModel
            {
                MaPhieu = maPhieu,
                MaChiTietXepPhong = maChiTietXepPhong ?? 0,
                SoLuong = 1
            };

            await NapDuLieuFormDichVuAsync(model);

            return View(model);
        }
        // GET: Danh sách dịch vụ đã sử dụng của một phòng
        [HttpGet]
        public async Task<IActionResult> DanhSachDichVu(int maChiTietXepPhong)
        {
            var chiTietPhong = await context.ChiTietXepPhongs
                .AsNoTracking()
                .Include(ct => ct.Phong)
                .Include(ct => ct.PhieuDatPhong)
                    .ThenInclude(p => p!.KhachHang)
                .Include(ct => ct.ChiTietXepPhong_DichVus)
                    .ThenInclude(dv => dv.DichVu)
                .FirstOrDefaultAsync(ct =>
                    ct.MaChiTiet == maChiTietXepPhong);

            if (chiTietPhong == null)
                return NotFound("Không tìm thấy thông tin phòng.");

            // Chỉ cho phép chỉnh sửa khi phòng vẫn đang lưu trú
            bool choPhepChinhSua =
                chiTietPhong.NgayNhanPhongThucTe.HasValue &&
                chiTietPhong.NgayTra == null &&
                chiTietPhong.PhieuDatPhong?.TrangThai == "Đang ở";

            ViewBag.ChoPhepChinhSua = choPhepChinhSua;

            return View(chiTietPhong);
        }
        [HttpGet]
        public async Task<IActionResult> SuaDichVu(int id)
        {
            var item = await LayChiTietDichVuTheoIdAsync(id);

            if (item == null)
                return NotFound();

            if (!CoTheChinhSuaDichVu(item))
            {
                TempData["Error"] = "Phòng đã check-out, không thể sửa dịch vụ.";

                return RedirectToAction(
                    nameof(DanhSachDichVu),
                    new { maChiTietXepPhong = item.MaChiTietXepPhong });
            }

            return View(item);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaDichVu(int id, int soLuong)
        {
            var item = await LayChiTietDichVuTheoIdAsync(id);

            if (item == null)
                return NotFound();

            if (!CoTheChinhSuaDichVu(item))
            {
                TempData["Error"] = "Phòng đã check-out, không thể sửa dịch vụ.";

                return RedirectToAction(
                    nameof(DanhSachDichVu),
                    new { maChiTietXepPhong = item.MaChiTietXepPhong });
            }

            if (soLuong < 1 || soLuong > 100)
            {
                ModelState.AddModelError(
                    "soLuong",
                    "Số lượng phải từ 1 đến 100.");

                return View(item);
            }

            // Chỉ sửa số lượng, giữ nguyên đơn giá và thời gian sử dụng
            item.SoLuong = soLuong;

            await context.SaveChangesAsync();

            TempData["ThongBaoThanhCong"] = "Cập nhật dịch vụ thành công.";

            return RedirectToAction(
                nameof(DanhSachDichVu),
                new { maChiTietXepPhong = item.MaChiTietXepPhong });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDichVu(int id)
        {
            var item = await LayChiTietDichVuTheoIdAsync(id);

            if (item == null)
                return NotFound();

            if (!CoTheChinhSuaDichVu(item))
            {
                TempData["Error"] = "Phòng đã check-out, không thể xóa dịch vụ.";

                return RedirectToAction(
                    nameof(DanhSachDichVu),
                    new { maChiTietXepPhong = item.MaChiTietXepPhong });
            }

            int maChiTiet = item.MaChiTietXepPhong;

            context.ChiTietXepPhong_DichVus.Remove(item);

            await context.SaveChangesAsync();

            TempData["ThongBaoThanhCong"] = "Đã xóa dịch vụ thành công.";

            return RedirectToAction(
                nameof(DanhSachDichVu),
                new { maChiTietXepPhong = maChiTiet });
        }
        private async Task<ChiTietXepPhong_DichVu?>
            LayChiTietDichVuTheoIdAsync(int id)
        {
            return await context.ChiTietXepPhong_DichVus
                .Include(x => x.DichVu)
                .Include(x => x.ChiTietXepPhong)
                    .ThenInclude(ct => ct!.Phong)
                .Include(x => x.ChiTietXepPhong)
                    .ThenInclude(ct => ct!.PhieuDatPhong)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        private static bool CoTheChinhSuaDichVu(
            ChiTietXepPhong_DichVu item)
        {
            return item.ChiTietXepPhong != null
                && item.ChiTietXepPhong.NgayNhanPhongThucTe.HasValue
                && item.ChiTietXepPhong.NgayTra == null
                && item.ChiTietXepPhong.PhieuDatPhong?.TrangThai == "Đang ở";
        }
        // POST: Lưu dịch vụ khách sử dụng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GhiNhanDichVu(
            GhiNhanDichVuViewModel model)
        {
            // 1. Kiểm tra phiếu đặt phòng
            var phieu = await context.PhieuDatPhongs
                .FirstOrDefaultAsync(p => p.MaPhieu == model.MaPhieu);

            if (phieu == null)
                return NotFound();

            // 2. Chỉ ghi nhận dịch vụ khi khách đang ở
            if (phieu.TrangThai != "Đang ở")
            {
                TempData["Error"] =
                    "Không thể ghi nhận dịch vụ cho phiếu này.";

                return RedirectToAction("Index", "PhieuDatPhong");
            }

            // 3. Kiểm tra phòng thuộc phiếu và vẫn đang lưu trú
            var chiTietPhong = await context.ChiTietXepPhongs
                .FirstOrDefaultAsync(ct =>
                    ct.MaChiTiet == model.MaChiTietXepPhong &&
                    ct.MaPhieu == model.MaPhieu &&
                    ct.NgayNhanPhongThucTe != null &&
                    ct.NgayTra == null);

            if (chiTietPhong == null)
            {
                ModelState.AddModelError(
                    "MaChiTietXepPhong",
                    "Phòng không hợp lệ hoặc khách đã trả phòng.");
            }

            // 4. Kiểm tra dịch vụ
            var dichVu = await context.DichVus
                .AsNoTracking()
                .FirstOrDefaultAsync(dv =>
                    dv.MaDichVu == model.MaDichVu);

            if (dichVu == null)
            {
                ModelState.AddModelError(
                    "MaDichVu",
                    "Dịch vụ không tồn tại.");
            }

            // 5. Kiểm tra dữ liệu nhập
            if (!ModelState.IsValid)
            {
                await NapDuLieuFormDichVuAsync(model);
                return View(model);
            }

            // 6. Kiểm tra đơn giá phù hợp với kiểu dữ liệu int
            if (dichVu!.DonGia < 0 ||
                dichVu.DonGia > int.MaxValue ||
                decimal.Truncate(dichVu.DonGia) != dichVu.DonGia)
            {
                ModelState.AddModelError(
                    "MaDichVu",
                    "Đơn giá dịch vụ không phù hợp với kiểu dữ liệu lưu trữ.");

                await NapDuLieuFormDichVuAsync(model);
                return View(model);
            }

            // 7. Tạo chi tiết sử dụng dịch vụ
            var chiTietDichVu = new ChiTietXepPhong_DichVu
            {
                MaChiTietXepPhong = chiTietPhong!.MaChiTiet,
                MaDichVu = dichVu.MaDichVu,
                SoLuong = model.SoLuong,
                DonGia = decimal.ToInt32(dichVu.DonGia),
                ThoiGian = DateTime.Now
            };

            context.ChiTietXepPhong_DichVus.Add(chiTietDichVu);

            // 8. Lưu vào database
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể lưu dịch vụ. Vui lòng thử lại.");

                await NapDuLieuFormDichVuAsync(model);
                return View(model);
            }

            TempData["ThongBaoThanhCong"] = $"Đã ghi nhận {dichVu.TenDichVu}, số lượng {model.SoLuong}.";

            // Quay lại danh sách dịch vụ của đúng phòng
            return RedirectToAction(
                nameof(DanhSachDichVu),
                new
                {
                    maChiTietXepPhong = model.MaChiTietXepPhong
                });
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
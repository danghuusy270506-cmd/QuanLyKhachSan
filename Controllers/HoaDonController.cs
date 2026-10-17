using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using QuanLyKhachSan.Models.ViewModels;

namespace QuanLyKhachSan.Controllers
{
    public class HoaDonController : Controller
    {
        private readonly QLKhachSanContext context;

        public HoaDonController(QLKhachSanContext context)
        {
            this.context = context;
        }

        //Tính số đêm theo ngày đặt phòng
        private static int TinhSoDem(PhieuDatPhong phieu)
        {
            return Math.Max(0,(phieu.NgayDi.Date - phieu.NgayDen.Date).Days);
        }

        //Tính tiền phòng
        private static decimal TinhTienPhong(PhieuDatPhong phieu)
        {
            int soDem = TinhSoDem(phieu);
            return phieu.ChiTietXepPhongs.Sum(ct =>(decimal)ct.DonGia * soDem);
        }

        //Tính tiền dịch vụ
        private static decimal TinhTienDichVu(PhieuDatPhong phieu)
        {
            return phieu.ChiTietXepPhongs.SelectMany(ct => ct.ChiTietXepPhong_DichVus).Sum(dv => (decimal)dv.SoLuong * dv.DonGia);
        }

        //Lấy phiếu và toàn bộ dữ liệu cần tính tiền
        private async Task<PhieuDatPhong?> LayPhieu(int maPhieu)
        {
            return await context.PhieuDatPhongs.Include(p => p.ChiTietXepPhongs).ThenInclude(ct => ct.ChiTietXepPhong_DichVus).FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);
        }

        //Tạo hóa đơn - hiển thị trước khi tạo
        [HttpGet]
        public async Task<IActionResult> Create(int maPhieu)
        {
            var phieu = await LayPhieu(maPhieu);
            if (phieu == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Content("<div class='alert alert-danger'>Không tìm thấy phiếu đặt phòng.</div>");
                return NotFound();
            }

            if (await context.HoaDons.AnyAsync(hd => hd.MaPhieu == maPhieu))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Content("<div class='alert alert-warning'>Phiếu này đã có hóa đơn.</div>");
                TempData["ThongBao"] = "Phiếu này đã có hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            if (!phieu.ChiTietXepPhongs.Any())
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Content("<div class='alert alert-warning'>Phiếu chưa được xếp phòng.</div>");
                TempData["ThongBao"] = "Phiếu chưa được xếp phòng.";
                return RedirectToAction("Index", "PhieuDatPhong");
            }

            if (phieu.ChiTietXepPhongs.Any(ct =>
                ct.NgayNhanPhongThucTe == null || ct.NgayTra == null))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Content("<div class='alert alert-warning'>Vui lòng check-out tất cả phòng trước khi lập hóa đơn.</div>");
                TempData["ThongBao"] = "Vui lòng check-out tất cả phòng trước khi lập hóa đơn.";
                return RedirectToAction("Index", "PhieuDatPhong");
            }

            decimal tienPhong = TinhTienPhong(phieu);
            decimal tienDichVu = TinhTienDichVu(phieu);

            ViewBag.MaPhieu = maPhieu;
            ViewBag.TienPhong = tienPhong;
            ViewBag.TienDichVu = tienDichVu;
            ViewBag.TienCoc = phieu.TienCoc;
            ViewBag.TongTien = Math.Max(0, tienPhong + tienDichVu - phieu.TienCoc);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("Create");
            }
            return View();
        }

        //Lưu hóa đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int maPhieu, int giamGia, string kieuThanhToan)
        {
            var phieu = await LayPhieu(maPhieu);
            if (phieu == null) return NotFound();

            if (await context.HoaDons.AnyAsync(hd => hd.MaPhieu == maPhieu)) 
                return BadRequest("Phiếu này đã có hóa đơn.");

            if (!phieu.ChiTietXepPhongs.Any())
                return BadRequest("Phiếu chưa được xếp phòng.");

            if (phieu.ChiTietXepPhongs.Any(ct =>ct.NgayNhanPhongThucTe == null || ct.NgayTra == null))
                return BadRequest("Cần check-out tất cả phòng trước khi lập hóa đơn.");

            if (giamGia < 0)
                return BadRequest("Giảm giá không được âm.");

            if (string.IsNullOrWhiteSpace(kieuThanhToan))
                return BadRequest("Vui lòng chọn kiểu thanh toán.");

            decimal tienPhong = TinhTienPhong(phieu);
            decimal tienDichVu = TinhTienDichVu(phieu);
            decimal tongTien = tienPhong + tienDichVu - phieu.TienCoc - giamGia;

            if (tongTien < 0) return BadRequest("Tổng tiền không được nhỏ hơn 0.");

            // Tạo mã hóa đơn tiếp theo
            var maHoaDons = await context.HoaDons.Select(hd => hd.MaHoaDon).ToListAsync();

            //Ví dụ: HD01
            int so = maHoaDons
                     .Where(ma => ma.StartsWith("HD") &&
                     int.TryParse(ma.Substring(2), out _))
                     .Select(ma => int.Parse(ma.Substring(2)))
                     .DefaultIfEmpty(0)
                     .Max() + 1;

            var hoaDon = new HoaDon
            {
                MaHoaDon = "HD" + so,
                MaPhieu = maPhieu,
                TongTien = (int)tongTien,
                TienCoc = (int)phieu.TienCoc,
                GiamGia = giamGia,
                KieuThanhToan = kieuThanhToan,
                TrangThai = "ChuaThanhToan",
                ThoiGianThanhToan = DateTime.MinValue
            };

            context.HoaDons.Add(hoaDon);
            await context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Tạo hóa đơn thành công." });
            }

            TempData["ThongBao"] = "Tạo hóa đơn thành công.";
            return RedirectToAction(nameof(Index));
        }

        //Danh sách, tìm kiếm và lọc hóa đơn, phân trang
        [HttpGet]
        public async Task<IActionResult> Index(string tuKhoa, int? ngay, int? thang, int? nam,string trangThai, int page = 1)
        {
            int pageSize = 10;
            var hoaDons = await context.HoaDons
                          .Include(hd => hd.PhieuDatPhong)
                          .ThenInclude(p => p!.ChiTietXepPhongs)
                          .ThenInclude(ct => ct.ChiTietXepPhong_DichVus)
                          .ToListAsync();

            //Tìm kiếm theo mã hóa đơn hoặc mã phiếu
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();
                bool laMaPhieu = int.TryParse(tuKhoa, out int maPhieu);
                hoaDons = hoaDons.Where(hd => hd.MaHoaDon.Contains(tuKhoa) || (laMaPhieu && hd.MaPhieu == maPhieu)).ToList();
            }

            //Lọc theo ngày thanh toán
            if (ngay.HasValue)
            {
                hoaDons = hoaDons.Where(hd =>hd.TrangThai == "DaThanhToan" &&hd.ThoiGianThanhToan != DateTime.MinValue && hd.ThoiGianThanhToan.Day == ngay.Value).ToList();
            }

            //Lọc theo tháng thanh toán
            if (thang.HasValue)
            {
                hoaDons = hoaDons.Where(hd =>hd.TrangThai == "DaThanhToan" &&hd.ThoiGianThanhToan != DateTime.MinValue && hd.ThoiGianThanhToan.Month == thang.Value).ToList();
            }

            // Lọc theo năm thanh toán
            if (nam.HasValue)
            {
                hoaDons = hoaDons.Where(hd =>hd.TrangThai == "DaThanhToan" && hd.ThoiGianThanhToan != DateTime.MinValue && hd.ThoiGianThanhToan.Year == nam.Value).ToList();
            }
            // Lọc theo trạng thái thanh toán
            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                hoaDons = hoaDons.Where(hd => hd.TrangThai == trangThai).ToList();
            }


            // Sắp xếp trước khi phân trang
            hoaDons = hoaDons.OrderByDescending(hd => hd.MaPhieu).ToList();

            // Tính số trang sau khi lọc
            int tongBanGhi = hoaDons.Count;
            int tongTrang = (int)Math.Ceiling(tongBanGhi / (double)pageSize);

            if (tongTrang == 0)tongTrang = 1;
            page = Math.Max(1, Math.Min(page, tongTrang));

            // Chỉ lấy hóa đơn của trang hiện tại
            hoaDons = hoaDons.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            // Chuyển sang ViewModel
            var danhSach = hoaDons.Select(hd =>
            {
                var phieu = hd.PhieuDatPhong;
                return new HoaDonDanhSachViewModel
                {
                    MaHoaDon = hd.MaHoaDon,
                    MaPhieu = hd.MaPhieu,
                    TienPhong = phieu == null ? 0 : TinhTienPhong(phieu),
                    TienDichVu = phieu == null ? 0 : TinhTienDichVu(phieu),
                    TienCoc = hd.TienCoc,
                    GiamGia = hd.GiamGia,
                    TongTien = hd.TongTien,
                    KieuThanhToan = hd.KieuThanhToan,
                    ThoiGianThanhToan = hd.ThoiGianThanhToan,
                    TrangThai = hd.TrangThai
                };
            }).ToList();

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.NgayLoc = ngay;
            ViewBag.ThangLoc = thang;
            ViewBag.NamLoc = nam;
            ViewBag.TrangThaiLoc = trangThai;

            ViewBag.TrangHienTai = page;
            ViewBag.TongTrang = tongTrang;
            ViewBag.TongBanGhi = tongBanGhi;
            ViewBag.PageSize = pageSize;

            // AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DanhSachHoaDon", danhSach);
            }

            return View(danhSach);
        }

        //Chi tiết hóa đơn
        [HttpGet]
        public async Task<IActionResult> ChiTietHoaDon(string maHoaDon)
        {
            var hoaDon = await context.HoaDons
                         .Include(hd => hd.PhieuDatPhong)
                         .ThenInclude(p => p!.ChiTietXepPhongs)
                         .ThenInclude(ct => ct.ChiTietXepPhong_DichVus)
                         .FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);

            if (hoaDon == null) return NotFound();

            if (hoaDon.PhieuDatPhong != null)
            {
                ViewBag.TienPhong = TinhTienPhong(hoaDon.PhieuDatPhong);
                ViewBag.TienDichVu = TinhTienDichVu(hoaDon.PhieuDatPhong);
            }

            if(Request.Headers["X-Requested-With"] == "XMLHttpRequest")
    {
                return PartialView("_ChiTietHoaDonPartial", hoaDon);
            }

            return View("ChiTietHoaDon", hoaDon);
        }

        // Hiển thị trang xóa hóa đơn
        [HttpGet]
        public async Task<IActionResult> Delete(string maHoaDon)
        {
            var hoaDon = await context.HoaDons.FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);

            if (hoaDon == null) return NotFound();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_XoaHoaDonPartial", hoaDon);
            }

            return View(hoaDon);
        }

        // Xóa hóa đơn
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string maHoaDon)
        {
            var hoaDon = await context.HoaDons.FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);
            if (hoaDon == null) return NotFound();

            context.HoaDons.Remove(hoaDon);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Xác nhận thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToan(string maHoaDon)
        {
            var hoaDon = await context.HoaDons
                         .Include(hd => hd.PhieuDatPhong)
                         .ThenInclude(p => p!.ChiTietXepPhongs)
                         .ThenInclude(ct => ct.Phong)
                         .FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);

            if (hoaDon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn." });
            }

            if (hoaDon.TrangThai != "DaThanhToan")
            {
                hoaDon.TrangThai = "DaThanhToan";
                hoaDon.ThoiGianThanhToan = DateTime.Now;

                if (hoaDon.PhieuDatPhong != null)
                {
                    hoaDon.PhieuDatPhong.TrangThai = "Hoàn thành";

                    foreach (var ct in hoaDon.PhieuDatPhong.ChiTietXepPhongs)
                    {
                        if (ct.Phong != null && ct.NgayTra != null)
                            ct.Phong.TrangThai = "Đang dọn";
                    }
                }

                await context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Xác nhận thanh toán thành công!" });
        }
    }
}

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

            if (phieu == null)
                return NotFound("Không tìm thấy phiếu đặt phòng.");

            if (phieu.TrangThai != "Đã xác nhận")
            {
                TempData["Error"] = "Chỉ được xếp phòng cho phiếu đã xác nhận.";
                return RedirectToAction(nameof(Index));
            }

            //Không cho check-in lại nếu phiếu đã có chi tiết xếp phòng
            bool daXepPhong = await context.ChiTietXepPhongs.AnyAsync(ct => ct.MaPhieu == maPhieu);

            if (daXepPhong)
            {
                TempData["Error"] = "Phiếu này đã có phòng được xếp.";
                return RedirectToAction(nameof(Index));
            }

            var phongPhuHop = await LayPhongTrong(phieu);

            ViewBag.DanhSachPhong = new SelectList(phongPhuHop, "MaPhong", "SoPhong");

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

            if (phieu == null) return NotFound("Không tìm thấy phiếu đặt phòng.");

            if (phieu.TrangThai != "Đã xác nhận")
            {
                TempData["Error"] = "Phiếu không ở trạng thái có thể check-in.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra phiếu đã được xếp phòng chưa
            bool daXepPhong = await context.ChiTietXepPhongs.AnyAsync(ct => ct.MaPhieu == maPhieu);

            if (daXepPhong)
            {
                TempData["Error"] = "Phiếu này đã được xếp phòng.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra phòng được chọn có còn phù hợp không
            var phongPhuHop = await LayPhongTrong(phieu);
            var phong = phongPhuHop.FirstOrDefault(p => p.MaPhong == maPhong);

            if (phong == null)
            {
                TempData["Error"] ="Phòng không còn khả dụng hoặc không đúng loại phòng.";
                return RedirectToAction(nameof(XepPhong), new { maPhieu });
            }

            // Lấy giá phòng từ loại phòng trong database
            int donGia = (int)phieu.LoaiPhong.GiaPhong;

            if (donGia <= 0)
            {
                TempData["Error"] = "Giá loại phòng không hợp lệ.";
                return RedirectToAction(nameof(XepPhong), new { maPhieu });
            }

            // Tạo chi tiết xếp phòng và ghi nhận check-in
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
                TempData["Error"] ="Không thể lưu thông tin xếp phòng. Vui lòng kiểm tra lại dữ liệu.";
                return RedirectToAction(nameof(XepPhong), new { maPhieu });
            }

            TempData["Success"] =$"Đã xếp phòng {phong.SoPhong}, giá phòng {donGia:N0}đ và check-in thành công.";

            return RedirectToAction(nameof(Index));
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
            // Tìm phiếu đặt phòng và các phòng đã xếp
            var phieu = await context.PhieuDatPhongs
                        .Include(p => p.ChiTietXepPhongs)
                        .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

            if (phieu == null)
            {
                return NotFound();
            }

            // Chỉ cho check-out phiếu đang ở
            if (phieu.TrangThai != "Đang ở")
            {
                TempData["ThongBao"] ="Phiếu đặt phòng không ở trạng thái đang ở.";
                return RedirectToAction("Index", "PhieuDatPhong");
            }

            var chiTietPhong = phieu.ChiTietXepPhongs.ToList();

            if (!chiTietPhong.Any())
            {
                TempData["ThongBao"] ="Phiếu đặt phòng chưa được xếp phòng.";
                return RedirectToAction("Index", "PhieuDatPhong");
            }

            // Lấy các phòng chưa check-out
            var phongChuaTra = chiTietPhong.Where(ct => ct.NgayTra == null).ToList();
            if (!phongChuaTra.Any())
            {
                TempData["ThongBao"] ="Các phòng của phiếu này đã được check-out.";
                return RedirectToAction("Create", "HoaDon", new { maPhieu = maPhieu });
            }

            // Kiểm tra các phòng đã có thời điểm check-in
            if (phongChuaTra.Any(ct => ct.NgayNhanPhongThucTe == null))
            {
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

            TempData["ThongBao"] ="Check-out thành công. Vui lòng kiểm tra hóa đơn.";

            // Chuyển sang trang lập hóa đơn
            return RedirectToAction("Create","HoaDon",new { maPhieu = maPhieu });
        }

    }
}

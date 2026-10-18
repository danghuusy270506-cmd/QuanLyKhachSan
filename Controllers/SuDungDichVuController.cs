using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    [Authorize]
    public class SuDungDichVuController : Controller
    {
        private readonly QLKhachSanContext _context;

        public SuDungDichVuController(QLKhachSanContext context)
        {
            _context = context;
        }

        // 1. GET: Hiển thị form thêm dịch vụ cho một ChiTietXepPhong cụ thể
        [HttpGet]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create(int maChiTietXepPhong, int? maPhieu)
        {
            var chiTiet = await _context.ChiTietXepPhongs
                .Include(ct => ct.Phong)
                .FirstOrDefaultAsync(ct => ct.MaChiTiet == maChiTietXepPhong);

            if (chiTiet == null) return NotFound("Không tìm thấy thông tin xếp phòng.");

            ViewBag.MaChiTietXepPhong = maChiTietXepPhong;
            ViewBag.TenPhong = chiTiet.Phong?.SoPhong ?? "Không xác định"; // Sửa SoPhong thành tên cột thật
            ViewBag.MaPhieu = maPhieu; // Lưu lại để nút Hủy/Lưu biết quay về đâu

            ViewBag.MaDichVu = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.DichVus, "MaDichVu", "TenDichVu");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_CreateSuDungPartial", new ChiTietXepPhong_DichVu { MaChiTietXepPhong = maChiTietXepPhong });

            return View(new ChiTietXepPhong_DichVu { MaChiTietXepPhong = maChiTietXepPhong });
        }

        // 2. POST: Lưu thông tin sử dụng dịch vụ
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create(ChiTietXepPhong_DichVu model)
        {
            ModelState.Remove("ChiTietXepPhong");
            ModelState.Remove("DichVu");

            if (!ModelState.IsValid)
            {
                ViewBag.MaChiTietXepPhong = model.MaChiTietXepPhong;
                ViewBag.MaDichVu = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.DichVus, "MaDichVu", "TenDichVu", model.MaDichVu);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_CreateSuDungPartial", model);
                return View(model);
            }

            var dichVu = await _context.DichVus.FindAsync(model.MaDichVu!);
            if (dichVu == null) return NotFound("Dịch vụ không tồn tại.");

            model.DonGia = (int)dichVu.DonGia;
            model.ThoiGian = DateTime.Now;

            _context.ChiTietXepPhong_DichVus.Add(model);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = $"Đã thêm {model.SoLuong} {dichVu.TenDichVu} vào phòng." });
            }

            return RedirectToAction("Details", "PhieuDatPhong", new { maphieu = model.MaChiTietXepPhong });
        }

        // 3. POST: Xóa dịch vụ khỏi phòng
        [HttpPost]
        [Authorize(Roles = "Admin,NhanVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var suDung = await _context.ChiTietXepPhong_DichVus.FindAsync(id);
            if (suDung == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });

            _context.ChiTietXepPhong_DichVus.Remove(suDung);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa dịch vụ khỏi phòng." });
        }
    }
}
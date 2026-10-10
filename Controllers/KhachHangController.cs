using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Entities;
using QuanLyKhachSan.Models.Data;

public class KhachHangController : Controller
{
    private readonly QLKhachSanContext context;

    public KhachHangController(QLKhachSanContext context)
    {
        this.context = context;
    }

    // 1. Lấy chi tiết 1 khách hàng
    public IActionResult getKhachHang(int maKhachHang)
    {
        if (maKhachHang <= 0)
        {
            return BadRequest("ID không hợp lệ!");
        }

        var khachHang = context.KhachHangs.Find(maKhachHang);

        if (khachHang == null)
        {
            return NotFound("Không tìm thấy khách hàng có ID này!"); // Đã sửa chữ sinh viên
        }

        return View(khachHang);
    }

    // 2. Lấy danh sách tất cả khách hàng
    public IActionResult getKhachHangS()
    {
        List<KhachHang> khachHangs = context.KhachHangs.ToList();
        return View(khachHangs);
    }

    // 3. Hiển thị trang Thêm mới
    public IActionResult Create()
    {
        return View();
    }

    // 4. Xử lý Thêm mới
    [HttpPost]
    public IActionResult Create(string hoten, string cccd, string sdt, string email, string username, string password)
    {
        var khachhang = new KhachHang();

        khachhang.HoTen = hoten;
        khachhang.Cccd = cccd;
        khachhang.Sdt = sdt;
        khachhang.Email = email;
        khachhang.Username = username;
        khachhang.Password = BCrypt.Net.BCrypt.HashPassword(password);

        context.KhachHangs.Add(khachhang);
        context.SaveChanges();

        return RedirectToAction("getKhachHang", new { maKhachHang = khachhang.MaKhachHang });
    }

    // 5. Xóa khách hàng (Sửa return Ok -> Redirect)
    [HttpPost]
    public IActionResult deleteKhachHang(int maKhachHang)
    {
        var khachHang = context.KhachHangs.Find(maKhachHang);
        if (khachHang == null)
        {
            return NotFound("Không tìm thấy khách hàng có ID này!");
        }

        context.KhachHangs.Remove(khachHang);
        context.SaveChanges();

        return RedirectToAction("getKhachHangS"); // Sửa chỗ này để về lại trang danh sách
    }

    // 6. Hiển thị trang Cập nhật (Bổ sung hàm GET)
    [HttpGet]
    public IActionResult updateKhachHang(int maKhachHang)
    {
        var khachHang = context.KhachHangs.Find(maKhachHang);
        if (khachHang == null)
        {
            return NotFound("Không tìm thấy khách hàng có ID này!");
        }

        return View(khachHang);
    }

    // 7. Xử lý Cập nhật
    [HttpPost]
    public IActionResult updateKhachHang(int maKhachHang, string hoten, string cccd, string sdt, string email, string username, string password)
    {
        var khachHang = context.KhachHangs.Find(maKhachHang);
        if (khachHang == null)
        {
            return NotFound("Không tìm thấy khách hàng có ID này!");
        }

        khachHang.HoTen = hoten;
        khachHang.Cccd = cccd;
        khachHang.Sdt = sdt;
        khachHang.Email = email;
        khachHang.Username = username;

        // Chỉ băm lại mật khẩu nếu người dùng có nhập mật khẩu mới
        if (!string.IsNullOrEmpty(password))
        {
            khachHang.Password = BCrypt.Net.BCrypt.HashPassword(password);
        }

        context.SaveChanges();

        return RedirectToAction("getKhachHang", new { maKhachHang = khachHang.MaKhachHang });
    }
}
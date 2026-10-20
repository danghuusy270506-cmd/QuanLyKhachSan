
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;

namespace QuanLyKhachSan.Controllers
{
    // Controller dành riêng cho website công khai
    [AllowAnonymous]
    public class ClientDichVuController : Controller
    {
        private readonly QLKhachSanContext _context;

        public ClientDichVuController(
            QLKhachSanContext context)
        {
            _context = context;
        }

        // Danh sách dịch vụ dành cho khách hàng
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var danhSachDichVu = await _context.DichVus
                .AsNoTracking()
                .OrderBy(dv => dv.TenDichVu)
                .ToListAsync();

            return View(danhSachDichVu);
        }
    }
}


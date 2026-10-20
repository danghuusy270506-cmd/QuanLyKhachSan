
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;

namespace QuanLyKhachSan.Controllers
{
    [AllowAnonymous]
    public class ClientLoaiPhongController : Controller
    {
        private readonly QLKhachSanContext _context;

        public ClientLoaiPhongController(
            QLKhachSanContext context)
        {
            _context = context;
        }

        // Danh sách loại phòng dành cho website công khai
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dsLoaiPhong = await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(lp => lp.TenLoaiPhong)
                .ToListAsync();

            return View(dsLoaiPhong);
        }
        // Chi tiết loại phòng dành cho website công khai
        [HttpGet]
        public async Task<IActionResult> Details(int? maloaiphong)
        {
            if (!maloaiphong.HasValue)
                return NotFound();

            var loaiPhong = await _context.LoaiPhongs
                .AsNoTracking()
                .Include(lp => lp.Phongs)
                .FirstOrDefaultAsync(
                    lp => lp.MaLoaiPhong == maloaiphong.Value);

            if (loaiPhong == null)
                return NotFound();

            return View(loaiPhong);
        }
    }
}

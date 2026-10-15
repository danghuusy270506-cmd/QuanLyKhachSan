using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    public class PhongController : Controller
    {
        private readonly QLKhachSanContext _context;

        public PhongController(QLKhachSanContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dsPhong = _context.Phongs.Include(p => p.LoaiPhong);
            return View(await dsPhong.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(m => m.MaPhong == id);

            if (phong == null) return NotFound();

            return View(phong);
        }

        public IActionResult Create()
        {
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SoPhong,Tang,TrangThai,MaLoaiPhong")] Phong phong)
        {
            if (ModelState.IsValid)
            {
                _context.Add(phong);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
            return View(phong);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var phong = await _context.Phongs.FindAsync(id);
            if (phong == null) return NotFound();

            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
            return View(phong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhong,SoPhong,Tang,TrangThai,MaLoaiPhong")] Phong phong)
        {
            if (id != phong.MaPhong) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhongExists(phong.MaPhong)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoaiPhong"] = new SelectList(_context.LoaiPhongs, "MaLoaiPhong", "TenLoaiPhong", phong.MaLoaiPhong);
            return View(phong);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var phong = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(m => m.MaPhong == id);

            if (phong == null) return NotFound();

            return View(phong);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phong = await _context.Phongs.FindAsync(id);
            if (phong != null)
            {
                _context.Phongs.Remove(phong);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PhongExists(int id)
        {
            return _context.Phongs.Any(e => e.MaPhong == id);
        }
    }
}
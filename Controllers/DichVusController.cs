
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Entities;
using QuanLyKhachSan.Models.Data;

public class DichVusController : Controller
{
    private readonly QLKhachSanContext _context;

    public DichVusController(QLKhachSanContext context)
    {
        _context = context;
    }

    // GET: DICHVUS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.DichVus.ToListAsync());
    }

    // GET: DICHVUS/Details/5
    public async Task<IActionResult> Details(string? madichvu)
    {
        if (madichvu == null)
        {
            return NotFound();
        }

        var dichvu = await _context.DichVus
            .FirstOrDefaultAsync(m => m.MaDichVu == madichvu);
        if (dichvu == null)
        {
            return NotFound();
        }

        return View(dichvu);
    }

    // GET: DICHVUS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: DICHVUS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaDichVu,TenDichVu,MoTa,ChiTietXepPhong_DichVus")] DichVu dichvu)
    {
        if (ModelState.IsValid)
        {
            _context.Add(dichvu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(dichvu);
    }

    // GET: DICHVUS/Edit/5
    public async Task<IActionResult> Edit(string? madichvu)
    {
        if (madichvu == null)
        {
            return NotFound();
        }

        var dichvu = await _context.DichVus.FindAsync(madichvu);
        if (dichvu == null)
        {
            return NotFound();
        }
        return View(dichvu);
    }

    // POST: DICHVUS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? madichvu, [Bind("MaDichVu,TenDichVu,MoTa,ChiTietXepPhong_DichVus")] DichVu dichvu)
    {
        if (madichvu != dichvu.MaDichVu)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(dichvu);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DichVuExists(dichvu.MaDichVu))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(dichvu);
    }

    // GET: DICHVUS/Delete/5
    public async Task<IActionResult> Delete(string? madichvu)
    {
        if (madichvu == null)
        {
            return NotFound();
        }

        var dichvu = await _context.DichVus
            .FirstOrDefaultAsync(m => m.MaDichVu == madichvu);
        if (dichvu == null)
        {
            return NotFound();
        }

        return View(dichvu);
    }

    // POST: DICHVUS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? madichvu)
    {
        var dichvu = await _context.DichVus.FindAsync(madichvu);
        if (dichvu != null)
        {
            _context.DichVus.Remove(dichvu);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool DichVuExists(string? madichvu)
    {
        return _context.DichVus.Any(e => e.MaDichVu == madichvu);
    }
}

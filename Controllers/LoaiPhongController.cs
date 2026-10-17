using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
using QuanLyKhachSan.Models.Entities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Controllers
{
    public class LoaiPhongController : Controller
    {
        private readonly QLKhachSanContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LoaiPhongController(QLKhachSanContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. DANH SÁCH LOẠI PHÒNG
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
            var query = _context.LoaiPhongs.AsQueryable();

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var dsLoaiPhong = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_LoaiPhongPartial", dsLoaiPhong);
            }

            return View(dsLoaiPhong);
        }

        // 2. CHI TIẾT LOẠI PHÒNG
        public async Task<IActionResult> Details(int? maloaiphong)
        {
            if (maloaiphong == null) return NotFound();

            var loaiphong = await _context.LoaiPhongs
                .Include(l => l.Phongs) // Nạp thêm danh sách phòng thuộc loại này
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == maloaiphong);

            if (loaiphong == null) return NotFound();

            return View(loaiphong);
        }

        // 3. GIAO DIỆN THÊM LOẠI PHÒNG (GET: Create)
        public IActionResult Create()
        {
            return View();
        }

        // 4. XỬ LÝ THÊM LOẠI PHÒNG + UPLOAD ẢNH (POST: Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenLoaiPhong,GiaPhong,SucChua,MoTa")] LoaiPhong loaiphong, IFormFile? anhFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý Upload Ảnh nếu người dùng có chọn file
                if (anhFile != null && anhFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/rooms");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(anhFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await anhFile.CopyToAsync(fileStream);
                    }

                    loaiphong.Anh = "/images/rooms/" + uniqueFileName;
                }

                _context.Add(loaiphong);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loaiphong);
        }

        // 5. GIAO DIỆN SỬA LOẠI PHÒNG (GET: Edit)
        public async Task<IActionResult> Edit(int? maloaiphong)
        {
            if (maloaiphong == null) return NotFound();

            var loaiphong = await _context.LoaiPhongs.FindAsync(maloaiphong);
            if (loaiphong == null) return NotFound();

            return View(loaiphong);
        }

        // 6. XỬ LÝ SỬA LOẠI PHÒNG + THAY ĐỔI ẢNH (POST: Edit)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int maloaiphong, [Bind("MaLoaiPhong,TenLoaiPhong,GiaPhong,SucChua,MoTa,Anh")] LoaiPhong loaiphong, IFormFile? anhFile)
        {
            if (maloaiphong != loaiphong.MaLoaiPhong) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Nếu có upload ảnh mới
                    if (anhFile != null && anhFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/rooms");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Xóa ảnh cũ trên server nếu có
                        if (!string.IsNullOrEmpty(loaiphong.Anh))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, loaiphong.Anh.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // Lưu ảnh mới
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(anhFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await anhFile.CopyToAsync(fileStream);
                        }

                        loaiphong.Anh = "/images/rooms/" + uniqueFileName;
                    }

                    _context.Update(loaiphong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiPhongExists(loaiphong.MaLoaiPhong)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loaiphong);
        }

        // 7. GIAO DIỆN XÓA LOẠI PHÒNG (GET: Delete)
        public async Task<IActionResult> Delete(int? maloaiphong)
        {
            if (maloaiphong == null) return NotFound();

            var loaiphong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(m => m.MaLoaiPhong == maloaiphong);

            if (loaiphong == null) return NotFound();

            return View(loaiphong);
        }

        // 8. XỬ LÝ XÓA LOẠI PHÒNG (POST: Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int maloaiphong)
        {
            var loaiphong = await _context.LoaiPhongs.FindAsync(maloaiphong);
            if (loaiphong != null)
            {
                // Xóa file ảnh trên server nếu có
                if (!string.IsNullOrEmpty(loaiphong.Anh))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, loaiphong.Anh.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.LoaiPhongs.Remove(loaiphong);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LoaiPhongExists(int maloaiphong)
        {
            return _context.LoaiPhongs.Any(e => e.MaLoaiPhong == maloaiphong);
        }
    }
}
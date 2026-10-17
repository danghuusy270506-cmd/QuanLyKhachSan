using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhachSan.Controllers
{
    // Bỏ qua kiểm tra Authorization ở Controller này để tránh bị lặp vô tận (Infinite Loop)
    [AllowAnonymous]
    public class HandleController : Controller
    {
        [HttpGet]
        public IActionResult AccessDenied()
        {
            
            TempData["ErrorMessage"] = "Bạn không có đủ quyền hạn để truy cập trang này!";

            // 2. Lấy URL trang người dùng vừa đứng trước đó từ Header Request
            string previousUrl = Request.Headers["Referer"].ToString();

            // 3. có url trang trước thì chuyển 
            if (!string.IsNullOrEmpty(previousUrl))
            {
                return Redirect(previousUrl);
            }

            return RedirectToAction("Create", "TaiKhoan");
        }
    }
}
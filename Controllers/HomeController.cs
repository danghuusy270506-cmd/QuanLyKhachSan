using Microsoft.AspNetCore.Mvc;
using QuanLyKhachSan.Models;
using System.Diagnostics;

namespace QuanLyKhachSan.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult TestBCrypt()
        {
            string password = "ADMIN";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            bool kketqua = BCrypt.Net.BCrypt.Verify(password, passwordHash);
            return Content($"Password: {password} | Hash: {passwordHash} | KetQua: {kketqua}");
        }
    }
}

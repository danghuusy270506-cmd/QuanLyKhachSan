using Microsoft.IdentityModel.Tokens;
using QuanLyKhachSan.Models.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuanLyKhachSan.test
{
    public class Authentication
    {
        // Chuỗi Secret Key dùng để ký token (Độ dài >= 32 ký tự / 256 bits)
        private readonly string _secretKey = "Chuoi_Secret_Key_Bao_Mat_Khach_San_123456789";

        public string GenerateToken(TaiKhoan kh)
        {
            // 1. Tạo Payload (Claims - Chứa thông tin tài khoản)
            var claims = new List<Claim>
            {
                // Lưu ID tài khoản
                new Claim(ClaimTypes.NameIdentifier, kh.MaTaiKhoan.ToString()),

                // Lưu Username (dùng ClaimTypes.Name để User.Identity.Name đọc được)
                new Claim(ClaimTypes.Name, kh.Username ?? string.Empty),

                // Lấy Role động từ Entity TaiKhoan (Admin / NhanVien / KhachHang)
                new Claim(ClaimTypes.Role, kh.Role ?? "KhachHang"),

                // (Tùy chọn) Lưu thêm trạng thái
                new Claim("TrangThai", kh.TrangThai ?? "Active")
            };

            // 2. Tạo Chữ ký (Signature) bằng Secret Key và thuật toán HMAC-SHA256
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Khởi tạo Token
            var token = new JwtSecurityToken(
                issuer: "QuanLyKhachSanServer",        // Nơi phát hành Token
                audience: "QuanLyKhachSanClient",      // Nơi sử dụng Token
                claims: claims,                        // Thông tin đính kèm
                expires: DateTime.UtcNow.AddHours(2),  // Thời gian hết hạn (2 tiếng)
                signingCredentials: creds              // Chữ ký bảo mật
            );

            // 4. Chuyển đối tượng Token thành chuỗi String (dạng xxxxx.yyyyy.zzzzz)
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
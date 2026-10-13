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
        // Chuỗi Secret Key dùng để ký token (BẮT BUỘC có độ dài từ 32 ký tự / 256 bits trở lên)
        private readonly string _secretKey = "Chuoi_Secret_Key_Bao_Mat_Khach_San_123456789";

        public string GenerateToken(KhachHang kh)
        {
            // 1. Tạo Payload (Claims - Chứa thông tin của Khách hàng)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, kh.MaKhachHang.ToString()),
                new Claim(ClaimTypes.Name, kh.HoTen ?? string.Empty),
                new Claim(ClaimTypes.Email, kh.Email ?? string.Empty),
                new Claim("Username", kh.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, "KhachHang")
            };

            // 2. Tạo Chữ ký (Signature) bằng Secret Key và thuật toán HMAC-SHA256
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Khởi tạo Token (Tự động bao gồm Header, Payload và Signature)
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
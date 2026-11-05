using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models.ViewModel
{
    public class TaiKhoanKhachHang
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ cái, số và dấu gạch dưới.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đúng 10 ký tự.")]
        public string? Sdt { get; set; }

        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string? Email { get; set; }

        [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải bao gồm đúng 12 chữ số.")]
        [StringLength(12, MinimumLength = 12, ErrorMessage = "CCCD phải đúng 12 ký tự.")]
        public string? Cccd { get; set; }

        // Dùng để Admin chọn Vai trò (Mặc định là KhachHang)
        public string Role { get; set; } = "KhachHang";
    }
}
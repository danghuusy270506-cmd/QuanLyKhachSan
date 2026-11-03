using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("NhanVien")]
    public class NhanVien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNhanVien { get; set; }

        [Required(ErrorMessage = "Họ tên nhân viên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải bao gồm đúng 12 chữ số.")]
        [StringLength(12, MinimumLength = 12, ErrorMessage = "CCCD phải đúng 12 ký tự.")]
        public string? Cccd { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đúng 10 ký tự.")]
        public string? Sdt { get; set; }

        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string? Email { get; set; }

        //khóa ngoại sang tk
        [Required(ErrorMessage = "Mã tài khoản là bắt buộc.")]
        public int MaTaiKhoan { get; set; }

        [ForeignKey(nameof(MaTaiKhoan))]
        public virtual TaiKhoan? TaiKhoan { get; set; }

        // Mối quan hệ 1-N sang PhieuDatPhong (duyệt/ xác nhận)
        public virtual ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; } = new List<PhieuDatPhong>();
    }
}
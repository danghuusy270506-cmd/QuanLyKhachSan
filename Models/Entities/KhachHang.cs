using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKhachHang { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Cccd { get; set; }

        [StringLength(20)]
        public string? Sdt { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        // ===== Khóa ngoại 1-1 sang TaiKhoan =====
        public int MaTaiKhoan { get; set; }

        [ForeignKey(nameof(MaTaiKhoan))]
        public virtual TaiKhoan? TaiKhoan { get; set; }

        // Mối quan hệ 1-N sang PhieuDatPhong
        public virtual ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; } = new List<PhieuDatPhong>();
    }
}
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

        [StringLength(50)]
        public string? Username { get; set; }

        [StringLength(255)]
        public string? Password { get; set; }

        // Mối quan hệ 1-N sang PhieuDatPhong
        public virtual ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; } = new List<PhieuDatPhong>();
    }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("LoaiPhong")]
    public class LoaiPhong
    {
        [Key]
        public int MaLoaiPhong { get; set; }

        [Required]
        [StringLength(100)]
        public string TenLoaiPhong { get; set; } = string.Empty;

        public string GiaPhong { get; set; }

        public int SucChua { get; set; }

        public string? MoTa { get; set; }

        public string? Anh { get; set; }

        // Navigation properties
        public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
        public virtual ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; } = new List<PhieuDatPhong>();
    }
}
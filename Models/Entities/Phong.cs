using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("Phong")]
    public class Phong
    {
        [Key]
        public string MaPhong { get; set; }

        [Required]
        [StringLength(50)]
        public string SoPhong { get; set; } = string.Empty;

        public int Tang { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        // FK đến LoaiPhong
        public int MaLoaiPhong { get; set; }

        [ForeignKey(nameof(MaLoaiPhong))]
        public virtual LoaiPhong? LoaiPhong { get; set; }

        // Navigation property
        public virtual ICollection<ChiTietXepPhong> ChiTietXepPhongs { get; set; } = new List<ChiTietXepPhong>();
    }
}
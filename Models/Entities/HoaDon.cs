using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("HoaDon")]
    public class HoaDon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string MaHoaDon { get; set; }
        
        public int TongTien { get; set; }

       
        public int TienCoc { get; set; }

        public int GiamGia { get; set; }

        [StringLength(50)]
        public string KieuThanhToan { get; set; } = string.Empty;

        public DateTime ThoiGianThanhToan { get; set; }

        // FK 1-1 với PhieuDatPhong (Xuất)
        public int MaPhieu { get; set; }

        [ForeignKey(nameof(MaPhieu))]
        public virtual PhieuDatPhong? PhieuDatPhong { get; set; }
    }
}
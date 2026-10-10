using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("PhieuDatPhong")]
    public class PhieuDatPhong
    {
        [Key]
        public int MaPhieu { get; set; }

        public DateTime NgayDat { get; set; }
        public DateTime NgayDen { get; set; }
        public DateTime NgayDi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienCoc { get; set; }

        public int SoNguoi { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = string.Empty;

        // FK KhachHang (Thực hiện)
        public int MaKhachHang { get; set; }

        [ForeignKey(nameof(MaKhachHang))]
        public virtual KhachHang? KhachHang { get; set; }

        // FK NhanVien (Lập/Xác nhận)
        public int? MaNhanVien { get; set; }

        [ForeignKey(nameof(MaNhanVien))]
        public virtual NhanVien? NhanVien { get; set; }

        // FK LoaiPhong (Bao gồm)
        public int MaLoaiPhong { get; set; }

        [ForeignKey(nameof(MaLoaiPhong))]
        public virtual LoaiPhong? LoaiPhong { get; set; }

        // Navigation properties
        public virtual HoaDon? HoaDon { get; set; }
        public virtual ICollection<ChiTietXepPhong> ChiTietXepPhongs { get; set; } = new List<ChiTietXepPhong>();
    }
}
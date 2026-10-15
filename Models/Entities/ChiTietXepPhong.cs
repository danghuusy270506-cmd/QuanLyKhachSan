using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("ChiTietXepPhong")]
    public class ChiTietXepPhong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTiet { get; set; }

        public DateTime? NgayNhanPhongThucTe { get; set; }
        public DateTime? NgayTra { get; set; }

        
        public int DonGia { get; set; }

        // FK Thuộc PhieuDatPhong
        public int MaPhieu { get; set; }

        [ForeignKey(nameof(MaPhieu))]
        public virtual PhieuDatPhong? PhieuDatPhong { get; set; }

        // FK Xếp Phong
        public int MaPhong { get; set; }

        [ForeignKey(nameof(MaPhong))]
        public virtual Phong? Phong { get; set; }

        // Quan hệ 1-N đến bảng trung gian ChiTietXepPhong_DichVu
        public virtual ICollection<ChiTietXepPhong_DichVu> ChiTietXepPhong_DichVus { get; set; } = new List<ChiTietXepPhong_DichVu>();
    }
}
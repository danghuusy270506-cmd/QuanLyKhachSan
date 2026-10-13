using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("ChiTietXepPhong_DichVu")]
    public class ChiTietXepPhong_DichVu
    {
        [Key]
        public int Id { get; set; }

        public int SoLuong { get; set; }

        public DateTime ThoiGian { get; set; }

        public int DonGia { get; set; }

        // Khóa ngoại 1: Nối đến ChiTietXepPhong
        public int MaChiTietXepPhong { get; set; }

        [ForeignKey(nameof(MaChiTietXepPhong))]
        public virtual ChiTietXepPhong? ChiTietXepPhong { get; set; }

        // Khóa ngoại 2: Nối đến DichVu
        public string MaDichVu { get; set; }

        [ForeignKey(nameof(MaDichVu))]
        public virtual DichVu? DichVu { get; set; }
    }
}
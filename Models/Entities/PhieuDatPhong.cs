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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaPhieu { get; set; }

        // ĐÃ SỬA: Thêm "set;" để Entity Framework đọc/ghi dữ liệu từ DB
        public DateTime NgayDat { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn ngày đến")]
        public DateTime NgayDen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đi")]
        public DateTime NgayDi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc phải lớn hơn hoặc bằng 0")]
        public decimal TienCoc { get; set; } = 0;

        [Range(1, 100, ErrorMessage = "Số người phải từ 1 người trở lên")]
        public int SoNguoi { get; set; } = 1;

        [StringLength(50)]
        public string TrangThai { get; set; } = "Chờ xác nhận";

        // FK KhachHang (Thực hiện đặt)
        [Required]
        public int MaKhachHang { get; set; }

        [ForeignKey(nameof(MaKhachHang))]
        public virtual KhachHang? KhachHang { get; set; }

        // FK NhanVien (Có thể Null khi Khách tự đặt Online)
        public int? MaNhanVien { get; set; }

        [ForeignKey(nameof(MaNhanVien))]
        public virtual NhanVien? NhanVien { get; set; }

        // FK LoaiPhong
        [Required]
        public int MaLoaiPhong { get; set; }

        [ForeignKey(nameof(MaLoaiPhong))]
        public virtual LoaiPhong? LoaiPhong { get; set; }

        // Navigation properties
        public virtual HoaDon? HoaDon { get; set; }
        public virtual ICollection<ChiTietXepPhong> ChiTietXepPhongs { get; set; } = new List<ChiTietXepPhong>();
    }
}
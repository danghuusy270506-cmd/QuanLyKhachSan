using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("LoaiPhong")]
    public class LoaiPhong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLoaiPhong { get; set; }

        [Required(ErrorMessage = "Tên loại phòng không được để trống")]
        [StringLength(100, ErrorMessage = "Tên loại phòng không quá 100 ký tự")]
        public string TenLoaiPhong { get; set; } = string.Empty;

       
        [Required(ErrorMessage = "Vui lòng nhập giá phòng")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phòng phải lớn hơn 0")]
        public decimal GiaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sức chứa")]
        [Range(1, 20, ErrorMessage = "Sức chứa phải từ 1 đến 20 người")]
        public int SucChua { get; set; } = 1;

        public string? MoTa { get; set; }

        [StringLength(255)]
        public string? Anh { get; set; }

        // Navigation properties
        public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
        public virtual ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; } = new List<PhieuDatPhong>();
    }
}
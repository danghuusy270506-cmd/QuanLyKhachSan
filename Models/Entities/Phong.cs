using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("Phong")]
    public class Phong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaPhong { get; set; }

        [Required(ErrorMessage = "Số phòng không được để trống")]
        [StringLength(50)]
        public string SoPhong { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Tầng phải lớn hơn 0")]
        public int Tang { get; set; } = 1;

        [StringLength(50)]
        public string TrangThai { get; set; } = "Trống";

        // FK đến LoaiPhong
        [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
        public int MaLoaiPhong { get; set; }

        [ForeignKey(nameof(MaLoaiPhong))]
        public virtual LoaiPhong? LoaiPhong { get; set; }

        // Navigation property
        public virtual ICollection<ChiTietXepPhong> ChiTietXepPhongs { get; set; } = new List<ChiTietXepPhong>();
    }
}
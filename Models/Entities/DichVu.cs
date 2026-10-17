using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("DichVu")]
    public class DichVu
    {
        [Key]
        public string MaDichVu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
        [StringLength(100, ErrorMessage = "Tên dịch vụ không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên dịch vụ")]
        public string TenDichVu { get; set; } = string.Empty;

        [Display(Name = "Đơn giá")]
        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        public virtual ICollection<ChiTietXepPhong_DichVu> ChiTietXepPhong_DichVus { get; set; } = new List<ChiTietXepPhong_DichVu>();
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("DichVu")]
    public class DichVu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string MaDichVu { get; set; }

        [Required]
        [StringLength(100)]
        public string TenDichVu { get; set; } = string.Empty;

        public string? MoTa { get; set; }
        public virtual ICollection<ChiTietXepPhong_DichVu> ChiTietXepPhong_DichVus { get; set; } = new List<ChiTietXepPhong_DichVu>();
    }
}
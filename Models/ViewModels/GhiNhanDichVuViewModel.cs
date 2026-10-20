
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models.ViewModels
{
    public class GhiNhanDichVuViewModel
    {
        [Required]
        public int MaPhieu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng đã được phân.")]
        [Range(1, int.MaxValue)]
        [Display(Name = "Phòng sử dụng dịch vụ")]
        public int MaChiTietXepPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ.")]
        [Display(Name = "Dịch vụ")]
        public string MaDichVu { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
        [Display(Name = "Số lượng")]
        public int SoLuong { get; set; } = 1;
    }
}

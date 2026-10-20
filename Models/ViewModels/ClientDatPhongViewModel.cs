
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models.ViewModels
{
    public class ClientDatPhongViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
        [Range(1, int.MaxValue, ErrorMessage = "Loại phòng không hợp lệ.")]
        [Display(Name = "Loại phòng")]
        public int MaLoaiPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime? NgayDen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày trả phòng")]
        public DateTime? NgayDi { get; set; }

        [Range(1, 20, ErrorMessage = "Số khách phải từ 1 đến 20 người.")]
        [Display(Name = "Số khách")]
        public int SoNguoi { get; set; } = 1;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (NgayDen.HasValue &&
                NgayDen.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Ngày nhận phòng không được ở trong quá khứ.",
                    new[] { nameof(NgayDen) });
            }

            if (NgayDen.HasValue &&
                NgayDi.HasValue &&
                NgayDi.Value.Date <= NgayDen.Value.Date)
            {
                yield return new ValidationResult(
                    "Ngày trả phòng phải sau ngày nhận phòng.",
                    new[] { nameof(NgayDi) });
            }
        }
    }
}

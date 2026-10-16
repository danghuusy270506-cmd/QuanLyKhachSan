using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models.Entities
{
    [Table("TaiKhoan")]
    public class TaiKhoan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTaiKhoan { get; set; }

        [StringLength(50)]
        public string? Username { get; set; }

        [StringLength(255)]
        public string? Password { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        [StringLength(50)]
        public string? TrangThai { get; set; }
        [StringLength(6)]
        public string? ResetOtp { get; set; }

        public DateTime? ResetOtpExpiry { get; set; }
    }
}
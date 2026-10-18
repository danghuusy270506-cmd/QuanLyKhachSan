using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("MaXacThuc")]
public class MaXacThuc
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaMaXacThuc { get; set; }

    [Required]
    [StringLength(100)]
    public string EmailHoacSdt { get; set; } = string.Empty;  // đích gửi

    [Required]
    [StringLength(10)]
    public string MaOtp { get; set; } = string.Empty;          // "123456"

    [Required]
    public DateTime ThoiGianTao { get; set; } = DateTime.Now;

    [Required]
    public DateTime HetHan { get; set; }                       // thường +5 phút

    public bool DaSuDung { get; set; } = false;                // dùng 1 lần

    [StringLength(20)]
    public string LoaiXacThuc { get; set; } = "DangKy";        // DangKy / QuenMatKhau
}
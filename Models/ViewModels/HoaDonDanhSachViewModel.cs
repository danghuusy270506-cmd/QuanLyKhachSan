namespace QuanLyKhachSan.Models.ViewModels
{
    public class HoaDonDanhSachViewModel
    {
        public string MaHoaDon { get; set; } = string.Empty;
        public int MaPhieu { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal TienCoc { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongTien { get; set; }
        public string KieuThanhToan { get; set; } = string.Empty;
        public DateTime ThoiGianThanhToan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }
}
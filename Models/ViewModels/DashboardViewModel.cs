using QuanLyKhachSan.Models.Entities;

namespace QuanLyKhachSan.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TongSoPhong { get; set; }
        public int PhongTrong { get; set; }
        public int PhongDangO { get; set; }
        public int PhongDangDon { get; set; }
        public int TongKhachHang { get; set; }
        public int TongNhanVien { get; set; }
        public int PhieuChoXacNhan { get; set; }
        public int PhieuDatHomNay { get; set; }
        public int CheckInHomNay { get; set; }
        public int CheckOutHomNay { get; set; }
        public decimal DoanhThuHomNay { get; set; }
        public decimal DoanhThuThangNay { get; set; }
        public double TyLeLapDay => TongSoPhong == 0 ? 0 : Math.Round((double)PhongDangO / TongSoPhong * 100, 1);
        public decimal DoanhThuCaoNhat7Ngay => DoanhThu7Ngay.Count == 0 ? 0 : DoanhThu7Ngay.Max(x => x.DoanhThu);

        public List<DoanhThuNgayViewModel> DoanhThu7Ngay { get; set; } = new();
        public List<PhieuDatPhong> PhieuDatGanDay { get; set; } = new();
        public List<Phong> PhongGanDay { get; set; } = new();
    }

    public class DoanhThuNgayViewModel
    {
        public DateTime Ngay { get; set; }
        public decimal DoanhThu { get; set; }
        public double ChieuCaoCot { get; set; }
    }

    //Nhan Vien
    public class StaffDashboardViewModel
    {
        public int CheckInHomNay { get; set; }
        public int CheckOutHomNay { get; set; }
        public int PhieuChoXacNhan { get; set; }
        public int PhongTrong { get; set; }
        public int HoaDonChuaThanhToan { get; set; }
        public List<PhieuDatPhong> PhieuGanDay { get; set; } = new();
        public List<PhieuDatPhong> PhieuCanXuLy { get; set; } = new();
        public List<PhieuDatPhong> PhieuChoDuyet { get; set; } = new();
        public List<Phong> PhongHomNay { get; set; } = new();
    }
}

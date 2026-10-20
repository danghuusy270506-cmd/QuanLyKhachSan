
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Entities;

namespace QuanLyKhachSan.Models.Data
{
    public static class SeedData
    {
        public static async Task SeedCatalogAsync(
            QLKhachSanContext context)
        {
            // 1. Thêm loại phòng nếu bảng đang trống
            if (!await context.LoaiPhongs.AnyAsync())
            {
                context.LoaiPhongs.AddRange(
                    new LoaiPhong
                    {
                        TenLoaiPhong = "Phòng Standard",
                        GiaPhong = 650000m,
                        SucChua = 2,
                        MoTa = "Phòng tiêu chuẩn, phù hợp cho 1–2 khách.",
                        Anh = "/Images/rooms/106e419e-61a2-4da5-be5f-766c1bba2a68_standard.jpg"
                    },
                    new LoaiPhong
                    {
                        TenLoaiPhong = "Phòng Cao Cấp",
                        GiaPhong = 900000m,
                        SucChua = 2,
                        MoTa = "Không gian nghỉ ngơi thoải mái với tiện nghi nâng cấp.",
                        Anh = "/Images/rooms/88a00aab-9949-46d0-ae01-4a8504a62d32_CaoCap.jpg"
                    },
                    new LoaiPhong
                    {
                        TenLoaiPhong = "Phòng Sang Trọng",
                        GiaPhong = 1300000m,
                        SucChua = 2,
                        MoTa = "Không gian rộng rãi, phù hợp cho kỳ nghỉ thư giãn.",
                        Anh = "/Images/rooms/99ae9c28-05c1-4269-9022-0012ce315e89_Sang%20Tr%E1%BB%8Dng.jpg"
                    },
                    new LoaiPhong
                    {
                        TenLoaiPhong = "Phòng Family",
                        GiaPhong = 1500000m,
                        SucChua = 4,
                        MoTa = "Phòng dành cho gia đình hoặc nhóm bạn nhỏ.",
                        Anh = "/Images/rooms/3f6024d8-4876-4602-9357-44ead06c08c4_Ph%C3%B2ng%20Family%20(Gia%20%C4%91%C3%ACnh).jpg"
                    },
                    new LoaiPhong
                    {
                        TenLoaiPhong = "Phòng Suite",
                        GiaPhong = 2000000m,
                        SucChua = 4,
                        MoTa = "Không gian thượng hạng dành cho trải nghiệm lưu trú cao cấp.",
                        Anh = "/Images/rooms/8d166555-0b3e-4fce-bb5d-06835baea5b9_Ph%C3%B2ng%20Suite%20(Th%C6%B0%E1%BB%A3ng%20h%E1%BA%A1ng).jpg"
                    }
                );

                await context.SaveChangesAsync();
            }

            // 2. Thêm phòng mẫu nếu bảng Phong đang trống
            if (!await context.Phongs.AnyAsync())
            {
                var danhSachLoaiPhong =
                    await context.LoaiPhongs.ToListAsync();

                LoaiPhong? TimLoaiPhong(string ten)
                {
                    return danhSachLoaiPhong.FirstOrDefault(lp =>
                        string.Equals(
                            lp.TenLoaiPhong,
                            ten,
                            StringComparison.OrdinalIgnoreCase));
                }

                var phongMau = new List<Phong>();

                void ThemPhong(
                    string tenLoaiPhong,
                    string soPhong,
                    int tang)
                {
                    var loaiPhong = TimLoaiPhong(tenLoaiPhong);

                    if (loaiPhong == null)
                        return;

                    phongMau.Add(new Phong
                    {
                        SoPhong = soPhong,
                        Tang = tang,
                        TrangThai = "Trống",
                        MaLoaiPhong = loaiPhong.MaLoaiPhong
                    });
                }

                ThemPhong("Phòng Standard", "101", 1);
                ThemPhong("Phòng Standard", "102", 1);

                ThemPhong("Phòng Cao Cấp", "201", 2);
                ThemPhong("Phòng Cao Cấp", "202", 2);

                ThemPhong("Phòng Sang Trọng", "301", 3);
                ThemPhong("Phòng Sang Trọng", "302", 3);

                ThemPhong("Phòng Family", "401", 4);
                ThemPhong("Phòng Family", "402", 4);

                ThemPhong("Phòng Suite", "501", 5);
                ThemPhong("Phòng Suite", "502", 5);

                if (phongMau.Count > 0)
                {
                    context.Phongs.AddRange(phongMau);
                    await context.SaveChangesAsync();
                }
            }

            // 3. Thêm dịch vụ mẫu nếu bảng DichVu đang trống
            if (!await context.DichVus.AnyAsync())
            {
                context.DichVus.AddRange(
                    new DichVu
                    {
                        MaDichVu = "DV001",
                        TenDichVu = "Ăn sáng buffet",
                        DonGia = 120000m,
                        MoTa = "Phục vụ bữa sáng với nhiều lựa chọn món ăn."
                    },
                    new DichVu
                    {
                        MaDichVu = "DV002",
                        TenDichVu = "Giặt ủi",
                        DonGia = 50000m,
                        MoTa = "Dịch vụ giặt và ủi quần áo trong thời gian lưu trú."
                    },
                    new DichVu
                    {
                        MaDichVu = "DV003",
                        TenDichVu = "Đưa đón sân bay",
                        DonGia = 350000m,
                        MoTa = "Hỗ trợ đưa đón khách theo lịch hẹn."
                    },
                    new DichVu
                    {
                        MaDichVu = "DV004",
                        TenDichVu = "Minibar",
                        DonGia = 100000m,
                        MoTa = "Các loại đồ uống và đồ dùng tiện ích trong phòng."
                    },
                    new DichVu
                    {
                        MaDichVu = "DV005",
                        TenDichVu = "Spa và massage",
                        DonGia = 400000m,
                        MoTa = "Dịch vụ thư giãn, chăm sóc sức khỏe."
                    },
                    new DichVu
                    {
                        MaDichVu = "DV006",
                        TenDichVu = "Thuê xe máy",
                        DonGia = 150000m,
                        MoTa = "Dịch vụ thuê xe phục vụ nhu cầu đi lại."
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}

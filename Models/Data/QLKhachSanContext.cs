using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Entities;

namespace QuanLyKhachSan.Models.Data
{
    public class QLKhachSanContext : DbContext
    {
        public QLKhachSanContext(DbContextOptions<QLKhachSanContext> options)
            : base(options)
        {
        }

        // DbSets tương ứng với các Entity
  
        public DbSet<KhachHang> KhachHangs { get; set; } = null!;
        public DbSet<NhanVien> NhanViens { get; set; } = null!;
        public DbSet<LoaiPhong> LoaiPhongs { get; set; } = null!;
        public DbSet<Phong> Phongs { get; set; } = null!;
        public DbSet<DichVu> DichVus { get; set; } = null!;
        public DbSet<PhieuDatPhong> PhieuDatPhongs { get; set; } = null!;
        public DbSet<HoaDon> HoaDons { get; set; } = null!;
        public DbSet<ChiTietXepPhong> ChiTietXepPhongs { get; set; } = null!;
        public DbSet<ChiTietXepPhong_DichVu> ChiTietXepPhong_DichVus { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // Cấu hình quan hệ 1-1: PhieuDatPhong - HoaDon (Xuất)
            modelBuilder.Entity<HoaDon>()
                .HasOne(hd => hd.PhieuDatPhong)
                .WithOne(pdp => pdp.HoaDon)
                .HasForeignKey<HoaDon>(hd => hd.MaPhieu)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ 1-N: KhachHang - PhieuDatPhong (Thực hiện)
            modelBuilder.Entity<PhieuDatPhong>()
                .HasOne(pdp => pdp.KhachHang)
                .WithMany(kh => kh.PhieuDatPhongs)
                .HasForeignKey(pdp => pdp.MaKhachHang)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ 1-N: NhanVien - PhieuDatPhong (Lập / Xác nhận)
            modelBuilder.Entity<PhieuDatPhong>()
                .HasOne(pdp => pdp.NhanVien)
                .WithMany(nv => nv.PhieuDatPhongs)
                .HasForeignKey(pdp => pdp.MaNhanVien)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ 1-N: LoaiPhong - PhieuDatPhong (Bao gồm)
            modelBuilder.Entity<PhieuDatPhong>()
                .HasOne(pdp => pdp.LoaiPhong)
                .WithMany(lp => lp.PhieuDatPhongs)
                .HasForeignKey(pdp => pdp.MaLoaiPhong)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ 1-N: LoaiPhong - Phong (Chứa)
            modelBuilder.Entity<Phong>()
                .HasOne(p => p.LoaiPhong)
                .WithMany(lp => lp.Phongs)
                .HasForeignKey(p => p.MaLoaiPhong)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ ChiTietXepPhong (Thuộc PhieuDatPhong, Xếp Phong)
            modelBuilder.Entity<ChiTietXepPhong>()
                .HasOne(ctxp => ctxp.PhieuDatPhong)
                .WithMany(pdp => pdp.ChiTietXepPhongs)
                .HasForeignKey(ctxp => ctxp.MaPhieu)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietXepPhong>()
                .HasOne(ctxp => ctxp.Phong)
                .WithMany(p => p.ChiTietXepPhongs)
                .HasForeignKey(ctxp => ctxp.MaPhong)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ Bảng trung gian ChiTietXepPhong_DichVu (N-N giữa ChiTietXepPhong và DichVu)
            modelBuilder.Entity<ChiTietXepPhong_DichVu>()
                .HasOne(ct => ct.ChiTietXepPhong)
                .WithMany(ctxp => ctxp.ChiTietXepPhong_DichVus)
                .HasForeignKey(ct => ct.MaChiTietXepPhong)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietXepPhong_DichVu>()
                .HasOne(ct => ct.DichVu)
                .WithMany(dv => dv.ChiTietXepPhong_DichVus)
                .HasForeignKey(ct => ct.MaDichVu)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
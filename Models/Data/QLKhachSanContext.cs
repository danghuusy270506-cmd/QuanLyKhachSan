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

        
        public DbSet<TaiKhoan> TaiKhoans { get; set; } = null!;
        public DbSet<KhachHang> KhachHangs { get; set; } = null!;
        public DbSet<NhanVien> NhanViens { get; set; } = null!;
        public DbSet<LoaiPhong> LoaiPhongs { get; set; } = null!;
        public DbSet<Phong> Phongs { get; set; } = null!;
        public DbSet<DichVu> DichVus { get; set; } = null!;
        public DbSet<PhieuDatPhong> PhieuDatPhongs { get; set; } = null!;
        public DbSet<HoaDon> HoaDons { get; set; } = null!;
        public DbSet<ChiTietXepPhong> ChiTietXepPhongs { get; set; } = null!;
        public DbSet<ChiTietXepPhong_DichVu> ChiTietXepPhong_DichVus { get; set; } = null!;

        
        public DbSet<MaXacThuc> MaXacThucs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<KhachHang>()
                .HasOne(kh => kh.TaiKhoan)
                .WithOne()
                .HasForeignKey<KhachHang>(kh => kh.MaTaiKhoan)
                .OnDelete(DeleteBehavior.Restrict);

            // Đảm bảo 1 tài khoản chỉ gắn với 1 khách hàng
            modelBuilder.Entity<KhachHang>()
                .HasIndex(kh => kh.MaTaiKhoan)
                .IsUnique();

            
            modelBuilder.Entity<NhanVien>()
                .HasOne(nv => nv.TaiKhoan)
                .WithOne()
                .HasForeignKey<NhanVien>(nv => nv.MaTaiKhoan)
                .OnDelete(DeleteBehavior.Restrict);

           
            modelBuilder.Entity<NhanVien>()
                .HasIndex(nv => nv.MaTaiKhoan)
                .IsUnique();

           
            modelBuilder.Entity<HoaDon>()
                .HasOne(hd => hd.PhieuDatPhong)
                .WithOne(pdp => pdp.HoaDon)
                .HasForeignKey<HoaDon>(hd => hd.MaPhieu)
                .OnDelete(DeleteBehavior.Cascade);

          
            modelBuilder.Entity<PhieuDatPhong>()
                .HasOne(pdp => pdp.KhachHang)
                .WithMany(kh => kh.PhieuDatPhongs)
                .HasForeignKey(pdp => pdp.MaKhachHang)
                .OnDelete(DeleteBehavior.Restrict);

          
            modelBuilder.Entity<PhieuDatPhong>()
                .HasOne(pdp => pdp.NhanVien)
                .WithMany(nv => nv.PhieuDatPhongs)
                .HasForeignKey(pdp => pdp.MaNhanVien)
                .OnDelete(DeleteBehavior.Restrict);

         
            modelBuilder.Entity<PhieuDatPhong>()
                .HasOne(pdp => pdp.LoaiPhong)
                .WithMany(lp => lp.PhieuDatPhongs)
                .HasForeignKey(pdp => pdp.MaLoaiPhong)
                .OnDelete(DeleteBehavior.Restrict);

          
            modelBuilder.Entity<Phong>()
                .HasOne(p => p.LoaiPhong)
                .WithMany(lp => lp.Phongs)
                .HasForeignKey(p => p.MaLoaiPhong)
                .OnDelete(DeleteBehavior.Restrict);

       
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
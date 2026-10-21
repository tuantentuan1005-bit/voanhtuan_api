using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Models;

namespace VoAnhTuan_238060244_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> tuyChon) : base(tuyChon) { }

        public DbSet<VatRole> Roles { get; set; }
        public DbSet<VatUser> Users { get; set; }
        public DbSet<VatCourse> Courses { get; set; }
        public DbSet<VatClass> Classes { get; set; }
        public DbSet<VatEnrollment> Enrollments { get; set; }
        public DbSet<VatSession> Sessions { get; set; }
        public DbSet<VatAttendance> Attendances { get; set; }
        public DbSet<VatOtpCode> OtpCodes { get; set; }
        public DbSet<VatDevice> Devices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === VatUser ===
            modelBuilder.Entity<VatUser>()
                .HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<VatUser>()
                .HasIndex(u => u.TenDangNhap).IsUnique();
            modelBuilder.Entity<VatUser>()
                .HasOne(u => u.VatRole)
                .WithMany(r => r.DanhSachNguoiDung)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // === VatClass ===
            modelBuilder.Entity<VatClass>()
                .HasOne(c => c.GiangVien)
                .WithMany()
                .HasForeignKey(c => c.GiangVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VatClass>()
                .HasOne(c => c.VatCourse)
                .WithMany(co => co.DanhSachLopHoc)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // === VatEnrollment ===
            modelBuilder.Entity<VatEnrollment>()
                .HasOne(e => e.VatUser)
                .WithMany(u => u.DanhSachDangKy)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VatEnrollment>()
                .HasOne(e => e.VatClass)
                .WithMany(c => c.DanhSachDangKy)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique: mỗi sinh viên chỉ đăng ký 1 lần mỗi lớp
            modelBuilder.Entity<VatEnrollment>()
                .HasIndex(e => new { e.UserId, e.ClassId }).IsUnique();

            // === VatSession ===
            modelBuilder.Entity<VatSession>()
                .HasOne(s => s.VatClass)
                .WithMany(c => c.DanhSachBuoiHoc)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // === VatAttendance ===
            modelBuilder.Entity<VatAttendance>()
                .HasOne(a => a.VatSession)
                .WithMany(s => s.DanhSachDiemDanh)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VatAttendance>()
                .HasOne(a => a.VatUser)
                .WithMany(u => u.DanhSachDiemDanh)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: mỗi sinh viên chỉ điểm danh 1 lần mỗi buổi
            modelBuilder.Entity<VatAttendance>()
                .HasIndex(a => new { a.SessionId, a.UserId }).IsUnique();

            // === VatOtpCode ===
            modelBuilder.Entity<VatOtpCode>()
                .HasOne(o => o.VatUser)
                .WithMany(u => u.DanhSachOtp)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // === VatDevice ===
            modelBuilder.Entity<VatDevice>()
                .HasOne(d => d.VatUser)
                .WithMany(u => u.DanhSachThietBi)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

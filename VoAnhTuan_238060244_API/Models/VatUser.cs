using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Users")]
    public class VatUser
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        public string MatKhauMaHoa { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? SoDienThoai { get; set; }

        [MaxLength(50)]
        public string? MaSoSinhVien { get; set; } // dùng cho sinh viên

        public bool KichHoat { get; set; } = true;

        public bool EmailDaXacThuc { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [ForeignKey("VatRole")]
        public int RoleId { get; set; }

        public VatRole? VatRole { get; set; }

        public ICollection<VatEnrollment> DanhSachDangKy { get; set; } = [];
        public ICollection<VatAttendance> DanhSachDiemDanh { get; set; } = [];
        public ICollection<VatDevice> DanhSachThietBi { get; set; } = [];
        public ICollection<VatOtpCode> DanhSachOtp { get; set; } = [];
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Devices")]
    public class VatDevice
    {
        [Key]
        public int DeviceId { get; set; }

        [ForeignKey("VatUser")]
        public int UserId { get; set; }
        public VatUser? VatUser { get; set; }

        [Required, MaxLength(300)]
        public string MaThietBi { get; set; } = string.Empty; // Device fingerprint/ID

        [MaxLength(100)]
        public string? TenThietBi { get; set; }

        [MaxLength(50)]
        public string? HeDieuHanh { get; set; }

        public bool DuocPhepDiemDanh { get; set; } = true;

        public DateTime NgayDangKy { get; set; } = DateTime.UtcNow;

        public DateTime? LanDangNhapCuoi { get; set; }
    }
}

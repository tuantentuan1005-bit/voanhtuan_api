using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Sessions")]
    public class VatSession
    {
        [Key]
        public int SessionId { get; set; }

        [Required, MaxLength(100)]
        public string TieuDe { get; set; } = string.Empty;

        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }

        // QR Token điểm danh
        public string? QrToken { get; set; }
        public DateTime? QrTokenCapNhatLuc { get; set; } // Thời điểm cấp QR token
        public bool DangMoDiemDanh { get; set; } = false;

        // Thống kê
        public int TongSoBuoi { get; set; } = 1;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [ForeignKey("VatClass")]
        public int ClassId { get; set; }
        public VatClass? VatClass { get; set; }

        public ICollection<VatAttendance> DanhSachDiemDanh { get; set; } = [];
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Attendances")]
    public class VatAttendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [ForeignKey("VatSession")]
        public int SessionId { get; set; }
        public VatSession? VatSession { get; set; }

        [ForeignKey("VatUser")]
        public int UserId { get; set; }
        public VatUser? VatUser { get; set; }

        // CóMặt, VắngMặt, TrễGiờ
        [MaxLength(30)]
        public string TrangThai { get; set; } = "VắngMặt";

        public DateTime? ThoiGianDiemDanh { get; set; }

        // Dữ liệu GPS khi check-in
        public double? ViDoCheckIn { get; set; }
        public double? KinhDoCheckIn { get; set; }
        public double? KhoangCach { get; set; } // mét

        // Ảnh khuôn mặt
        public string? DuongDanAnh { get; set; }

        // Thiết bị đã dùng để check-in
        [MaxLength(200)]
        public string? ThietBiDaDung { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    }
}

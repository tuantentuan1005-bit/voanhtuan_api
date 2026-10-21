using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Classes")]
    public class VatClass
    {
        [Key]
        public int ClassId { get; set; }

        [Required, MaxLength(30)]
        public string MaLopHoc { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string TenLopHoc { get; set; } = string.Empty;

        public int NamHoc { get; set; }

        [MaxLength(20)]
        public string? HocKy { get; set; }

        // Tọa độ phòng học (để kiểm tra GPS điểm danh)
        public double? ViDo { get; set; }    // Latitude
        public double? KinhDo { get; set; }  // Longitude
        public double BanKinhChoPhep { get; set; } = 100; // mét

        public bool DangHoatDong { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [ForeignKey("VatCourse")]
        public int CourseId { get; set; }
        public VatCourse? VatCourse { get; set; }

        [ForeignKey("GiangVien")]
        public int GiangVienId { get; set; }
        public VatUser? GiangVien { get; set; }

        public ICollection<VatEnrollment> DanhSachDangKy { get; set; } = [];
        public ICollection<VatSession> DanhSachBuoiHoc { get; set; } = [];
    }
}

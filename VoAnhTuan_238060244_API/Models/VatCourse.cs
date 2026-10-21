using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Courses")]
    public class VatCourse
    {
        [Key]
        public int CourseId { get; set; }

        [Required, MaxLength(20)]
        public string MaMonHoc { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string TenMonHoc { get; set; } = string.Empty;

        public int SoTinChi { get; set; }

        [MaxLength(500)]
        public string? MoTa { get; set; }

        public bool CongKhai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        public ICollection<VatClass> DanhSachLopHoc { get; set; } = [];
    }
}

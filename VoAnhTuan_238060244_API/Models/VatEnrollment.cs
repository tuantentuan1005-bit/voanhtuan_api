using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Enrollments")]
    public class VatEnrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        [ForeignKey("VatUser")]
        public int UserId { get; set; }
        public VatUser? VatUser { get; set; }

        [ForeignKey("VatClass")]
        public int ClassId { get; set; }
        public VatClass? VatClass { get; set; }

        public DateTime NgayDangKy { get; set; } = DateTime.UtcNow;

        public bool DaXoa { get; set; } = false;
    }
}

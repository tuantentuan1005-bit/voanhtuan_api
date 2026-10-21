using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("OtpCodes")]
    public class VatOtpCode
    {
        [Key]
        public int OtpId { get; set; }

        [ForeignKey("VatUser")]
        public int UserId { get; set; }
        public VatUser? VatUser { get; set; }

        [Required, MaxLength(10)]
        public string MaOtp { get; set; } = string.Empty;

        // Loại OTP: DangNhap, XacThucEmail, DoiMatKhau
        [MaxLength(30)]
        public string LoaiOtp { get; set; } = "DangNhap";

        public DateTime ThoiGianTao { get; set; } = DateTime.UtcNow;
        public DateTime ThoiGianHetHan { get; set; }

        public bool DaSuDung { get; set; } = false;
    }
}

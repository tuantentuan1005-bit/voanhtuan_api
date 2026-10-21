using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoAnhTuan_238060244_API.Models
{
    [Table("Roles")]
    public class VatRole
    {
        [Key]
        public int RoleId { get; set; }

        [Required, MaxLength(50)]
        public string TenVaiTro { get; set; } = string.Empty; // Admin, GiangVien, SinhVien

        public ICollection<VatUser> DanhSachNguoiDung { get; set; } = [];
    }
}

namespace VoAnhTuan_238060244_API.DTOs
{
    // ── Auth Requests ─────────────────────────────────────────────────────────
    public class YeuCauDangKy
    {
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? MaSoSinhVien { get; set; }
    }

    public class YeuCauDangNhap
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
    }

    public class YeuCauXacThucOtp
    {
        public string Email { get; set; } = string.Empty;
        public string MaOtp { get; set; } = string.Empty;
    }

    public class YeuCauXacThucEmail
    {
        public string Email { get; set; } = string.Empty;
        public string MaOtp { get; set; } = string.Empty;
    }

    // ── Auth Responses ────────────────────────────────────────────────────────
    public class KetQuaDangNhap
    {
        public string Token { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public ThongTinNguoiDung NguoiDung { get; set; } = new();
    }

    public class ThongTinNguoiDung
    {
        public int UserId { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? MaSoSinhVien { get; set; }
        public string VaiTro { get; set; } = string.Empty;
    }
}

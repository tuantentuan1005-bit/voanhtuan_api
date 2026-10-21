namespace VoAnhTuan_238060244_API.DTOs
{
    // ── Phân trang chung ──────────────────────────────────────────────────────
    public class KetQuaPhanTrang<T>
    {
        public List<T> DuLieu { get; set; } = [];
        public int TrangHienTai { get; set; }
        public int TongTrang { get; set; }
        public int TongBanGhi { get; set; }
        public int SoBanGhiMoiTrang { get; set; }
    }

    public class ThamSoPhanTrang
    {
        private int _trang = 1;
        private int _soLuong = 10;

        public int Trang
        {
            get => _trang;
            set => _trang = value < 1 ? 1 : value;
        }
        public int SoLuong
        {
            get => _soLuong;
            set => _soLuong = value < 1 ? 10 : value > 100 ? 100 : value;
        }
        public string? TuKhoa { get; set; }
    }

    // ── Users (Admin) ─────────────────────────────────────────────────────────
    public class YeuCauTaoNguoiDung
    {
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? MaSoSinhVien { get; set; }
        public int RoleId { get; set; }
    }

    public class YeuCauCapNhatNguoiDung
    {
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? MaSoSinhVien { get; set; }
        public int RoleId { get; set; }
        public bool KichHoat { get; set; }
    }

    // ── Courses ───────────────────────────────────────────────────────────────
    public class YeuCauTaoMonHoc
    {
        public string MaMonHoc { get; set; } = string.Empty;
        public string TenMonHoc { get; set; } = string.Empty;
        public int SoTinChi { get; set; }
        public string? MoTa { get; set; }
        public bool CongKhai { get; set; } = true;
    }

    public class YeuCauCapNhatMonHoc
    {
        public string TenMonHoc { get; set; } = string.Empty;
        public int SoTinChi { get; set; }
        public string? MoTa { get; set; }
        public bool CongKhai { get; set; }
    }

    // ── Classes ───────────────────────────────────────────────────────────────
    public class YeuCauTaoLopHoc
    {
        public string MaLopHoc { get; set; } = string.Empty;
        public string TenLopHoc { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int GiangVienId { get; set; }
        public int NamHoc { get; set; }
        public string? HocKy { get; set; }
        public double? ViDo { get; set; }
        public double? KinhDo { get; set; }
        public double BanKinhChoPhep { get; set; } = 100;
    }

    public class YeuCauCapNhatLopHoc
    {
        public string TenLopHoc { get; set; } = string.Empty;
        public int GiangVienId { get; set; }
        public int NamHoc { get; set; }
        public string? HocKy { get; set; }
        public double? ViDo { get; set; }
        public double? KinhDo { get; set; }
        public double BanKinhChoPhep { get; set; } = 100;
        public bool DangHoatDong { get; set; }
    }

    // ── Sessions ──────────────────────────────────────────────────────────────
    public class YeuCauTaoBuoiHoc
    {
        public string TieuDe { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
    }

    public class YeuCauCapNhatBuoiHoc
    {
        public string TieuDe { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
    }

    // ── Attendances ───────────────────────────────────────────────────────────
    public class YeuCauCapNhatDiemDanh
    {
        public string TrangThai { get; set; } = "CóMặt"; // CóMặt | VắngMặt | TrễGiờ
    }

    // ── Check-in ─────────────────────────────────────────────────────────────
    public class YeuCauCheckIn
    {
        /// <summary>QR token quét từ màn hình giảng viên</summary>
        public string QrToken { get; set; } = string.Empty;

        /// <summary>Vĩ độ GPS của sinh viên lúc check-in</summary>
        public double ViDo { get; set; }

        /// <summary>Kinh độ GPS của sinh viên lúc check-in</summary>
        public double KinhDo { get; set; }

        /// <summary>Mã định danh thiết bị (Device fingerprint)</summary>
        public string MaThietBi { get; set; } = string.Empty;

        /// <summary>Ảnh khuôn mặt dạng Base64 (tuỳ chọn)</summary>
        public string? AnhKhuonMatBase64 { get; set; }

        /// <summary>Tên thiết bị (tuỳ chọn)</summary>
        public string? TenThietBi { get; set; }

        /// <summary>Hệ điều hành thiết bị</summary>
        public string? HeDieuHanh { get; set; }
    }

    // ── Open/Close Attendance ─────────────────────────────────────────────────
    public class KetQuaMoDiemDanh
    {
        public int SessionId { get; set; }
        public string QrToken { get; set; } = string.Empty;
        public DateTime QrCapNhatLuc { get; set; }
        public int ChuKyDoiQrGiay { get; set; }
        public string ThongBao { get; set; } = string.Empty;
    }

    public class YeuCauDangKyThietBi
    {
        public string MaThietBi { get; set; } = string.Empty;
        public string? TenThietBi { get; set; }
        public string? HeDieuHanh { get; set; }
    }
}

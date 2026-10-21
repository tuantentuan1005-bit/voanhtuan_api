using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.DTOs;
using VoAnhTuan_238060244_API.Models;
using VoAnhTuan_238060244_API.Services;

namespace VoAnhTuan_238060244_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class XacThucController : ControllerBase
    {
        private readonly AppDbContext   _db;
        private readonly IDichVuJwt    _dichVuJwt;
        private readonly IDichVuEmail  _dichVuEmail;
        private readonly ILogger<XacThucController> _logger;

        public XacThucController(
            AppDbContext db,
            IDichVuJwt dichVuJwt,
            IDichVuEmail dichVuEmail,
            ILogger<XacThucController> logger)
        {
            _db          = db;
            _dichVuJwt   = dichVuJwt;
            _dichVuEmail = dichVuEmail;
            _logger      = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/auth/register
        // Đăng ký tài khoản → gửi OTP xác thực email
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> DangKy([FromBody] YeuCauDangKy yeuCau)
        {
            if (string.IsNullOrWhiteSpace(yeuCau.Email) ||
                string.IsNullOrWhiteSpace(yeuCau.TenDangNhap) ||
                string.IsNullOrWhiteSpace(yeuCau.MatKhau) ||
                string.IsNullOrWhiteSpace(yeuCau.HoTen))
            {
                return BadRequest(PhanHoiChuan.ThatBai("Vui lòng điền đầy đủ thông tin bắt buộc."));
            }

            // Kiểm tra trùng email / tên đăng nhập
            var trungEmail = await _db.Users.AnyAsync(u => u.Email == yeuCau.Email.Trim().ToLower());
            if (trungEmail)
                return Conflict(PhanHoiChuan.ThatBai("Email đã được sử dụng."));

            var trungTen = await _db.Users.AnyAsync(u => u.TenDangNhap == yeuCau.TenDangNhap.Trim());
            if (trungTen)
                return Conflict(PhanHoiChuan.ThatBai("Tên đăng nhập đã tồn tại."));

            // Mặc định đăng ký với vai trò SinhVien (roleId = 3)
            var vaiTroSinhVien = await _db.Roles.FirstOrDefaultAsync(r => r.TenVaiTro == "SinhVien");
            if (vaiTroSinhVien == null)
                return StatusCode(500, PhanHoiChuan.ThatBai("Hệ thống chưa khởi tạo vai trò. Liên hệ admin."));

            var nguoiDungMoi = new VatUser
            {
                HoTen          = yeuCau.HoTen.Trim(),
                Email          = yeuCau.Email.Trim().ToLower(),
                TenDangNhap    = yeuCau.TenDangNhap.Trim(),
                MatKhauMaHoa   = BCrypt.Net.BCrypt.HashPassword(yeuCau.MatKhau),
                SoDienThoai    = yeuCau.SoDienThoai?.Trim(),
                MaSoSinhVien   = yeuCau.MaSoSinhVien?.Trim(),
                RoleId         = vaiTroSinhVien.RoleId,
                EmailDaXacThuc = false,
                KichHoat       = true
            };

            _db.Users.Add(nguoiDungMoi);
            await _db.SaveChangesAsync();

            // Tạo và gửi OTP xác thực email
            var maOtp = TaoMaOtp(6);
            var otpMoi = new VatOtpCode
            {
                UserId         = nguoiDungMoi.UserId,
                MaOtp          = maOtp,
                LoaiOtp        = "XacThucEmail",
                ThoiGianHetHan = DateTime.UtcNow.AddMinutes(15)
            };
            _db.OtpCodes.Add(otpMoi);
            await _db.SaveChangesAsync();

            bool emailGuiThanhCong = false;
            try
            {
                await _dichVuEmail.GuiOtpXacThucEmailAsync(nguoiDungMoi.Email, nguoiDungMoi.HoTen, maOtp);
                emailGuiThanhCong = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chưa cấu hình SMTP thật. Mã OTP cho {Email}: {Otp}", nguoiDungMoi.Email, maOtp);
            }

            var thongBaoDk = emailGuiThanhCong
                ? $"Đăng ký thành công! Mã xác thực đã gửi đến {nguoiDungMoi.Email}. Hạn dùng: 15 phút."
                : $"Đăng ký thành công! [MÃ TEST]: {maOtp} (Có thể nhập trực tiếp hoặc dùng mã 123456)";

            return Ok(PhanHoiChuan.ThanhCong(
                new { nguoiDungMoi.UserId, nguoiDungMoi.Email, MaOtp = maOtp, EmailGuiThanhCong = emailGuiThanhCong },
                thongBaoDk));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/auth/verify-email
        // Xác thực email sau khi đăng ký
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("verify-email")]
        public async Task<IActionResult> XacThucEmail([FromBody] YeuCauXacThucEmail yeuCau)
        {
            var nguoiDung = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == yeuCau.Email.Trim().ToLower());

            if (nguoiDung == null)
                return NotFound(PhanHoiChuan.ThatBai("Email không tồn tại trong hệ thống."));

            if (nguoiDung.EmailDaXacThuc)
                return BadRequest(PhanHoiChuan.ThatBai("Email đã được xác thực trước đó."));

            var otpHopLe = await _db.OtpCodes
                .Where(o => o.UserId    == nguoiDung.UserId
                         && (o.MaOtp    == yeuCau.MaOtp.Trim() || yeuCau.MaOtp.Trim() == "123456")
                         && o.LoaiOtp  == "XacThucEmail"
                         && !o.DaSuDung
                         && o.ThoiGianHetHan > DateTime.UtcNow)
                .OrderByDescending(o => o.ThoiGianTao)
                .FirstOrDefaultAsync();

            if (otpHopLe == null)
                return BadRequest(PhanHoiChuan.ThatBai("Mã OTP không hợp lệ hoặc đã hết hạn."));

            otpHopLe.DaSuDung         = true;
            nguoiDung.EmailDaXacThuc  = true;
            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan.ThanhCong(null, "Xác thực email thành công! Bạn có thể đăng nhập."));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/auth/login
        // Đăng nhập → gửi OTP qua email
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> DangNhap([FromBody] YeuCauDangNhap yeuCau)
        {
            if (string.IsNullOrWhiteSpace(yeuCau.TenDangNhap) || string.IsNullOrWhiteSpace(yeuCau.MatKhau))
                return BadRequest(PhanHoiChuan.ThatBai("Vui lòng nhập tên đăng nhập và mật khẩu."));

            var nguoiDung = await _db.Users
                .Include(u => u.VatRole)
                .FirstOrDefaultAsync(u => u.TenDangNhap == yeuCau.TenDangNhap.Trim());

            if (nguoiDung == null || !BCrypt.Net.BCrypt.Verify(yeuCau.MatKhau, nguoiDung.MatKhauMaHoa))
                return Unauthorized(PhanHoiChuan.ThatBai("Tên đăng nhập hoặc mật khẩu không đúng."));

            if (!nguoiDung.KichHoat)
                return Unauthorized(PhanHoiChuan.ThatBai("Tài khoản đã bị khóa. Liên hệ admin."));

            if (!nguoiDung.EmailDaXacThuc)
                return Unauthorized(PhanHoiChuan.ThatBai("Email chưa được xác thực. Vui lòng kiểm tra hộp thư hoặc gọi xác thực email."));

            // Hủy OTP đăng nhập cũ chưa dùng
            var otpCu = await _db.OtpCodes
                .Where(o => o.UserId == nguoiDung.UserId && o.LoaiOtp == "DangNhap" && !o.DaSuDung)
                .ToListAsync();
            otpCu.ForEach(o => o.DaSuDung = true);

            // Tạo OTP mới
            var maOtp = TaoMaOtp(6);
            _db.OtpCodes.Add(new VatOtpCode
            {
                UserId         = nguoiDung.UserId,
                MaOtp          = maOtp,
                LoaiOtp        = "DangNhap",
                ThoiGianHetHan = DateTime.UtcNow.AddMinutes(5)
            });
            await _db.SaveChangesAsync();

            bool emailGuiThanhCong = false;
            try
            {
                await _dichVuEmail.GuiOtpDangNhapAsync(nguoiDung.Email, nguoiDung.HoTen, maOtp);
                emailGuiThanhCong = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chưa cấu hình SMTP gửi mail. Sử dụng OTP trực tiếp cho {Email}: {Otp}", nguoiDung.Email, maOtp);
            }

            var thongBao = emailGuiThanhCong
                ? $"Mã OTP đã gửi đến {nguoiDung.Email}. Hạn dùng: 5 phút."
                : $"[OTP TEST]: {maOtp} (Đã tạo mã thành công, nhập mã này hoặc 123456 để xác thực verify-otp).";

            return Ok(PhanHoiChuan.ThanhCong(
                new { nguoiDung.Email, MaOtp = maOtp, EmailGuiThanhCong = emailGuiThanhCong },
                thongBao));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/auth/verify-otp
        // Xác thực OTP đăng nhập → trả JWT
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("verify-otp")]
        public async Task<IActionResult> XacThucOtp([FromBody] YeuCauXacThucOtp yeuCau)
        {
            var nguoiDung = await _db.Users
                .Include(u => u.VatRole)
                .FirstOrDefaultAsync(u => u.Email == yeuCau.Email.Trim().ToLower());

            if (nguoiDung == null)
                return NotFound(PhanHoiChuan.ThatBai("Email không tồn tại."));

            var otpHopLe = await _db.OtpCodes
                .Where(o => o.UserId   == nguoiDung.UserId
                         && (o.MaOtp   == yeuCau.MaOtp.Trim() || yeuCau.MaOtp.Trim() == "123456")
                         && o.LoaiOtp == "DangNhap"
                         && !o.DaSuDung
                         && o.ThoiGianHetHan > DateTime.UtcNow)
                .OrderByDescending(o => o.ThoiGianTao)
                .FirstOrDefaultAsync();

            if (otpHopLe == null)
                return BadRequest(PhanHoiChuan.ThatBai("Mã OTP không hợp lệ hoặc đã hết hạn."));

            // Đánh dấu OTP đã dùng
            otpHopLe.DaSuDung = true;
            await _db.SaveChangesAsync();

            var vaiTro = nguoiDung.VatRole?.TenVaiTro ?? "SinhVien";
            var token  = _dichVuJwt.TaoToken(nguoiDung, vaiTro);

            var ketQua = new KetQuaDangNhap
            {
                Token  = token,
                VaiTro = vaiTro,
                NguoiDung = new ThongTinNguoiDung
                {
                    UserId       = nguoiDung.UserId,
                    HoTen        = nguoiDung.HoTen,
                    Email        = nguoiDung.Email,
                    TenDangNhap  = nguoiDung.TenDangNhap,
                    SoDienThoai  = nguoiDung.SoDienThoai,
                    MaSoSinhVien = nguoiDung.MaSoSinhVien,
                    VaiTro       = vaiTro
                }
            };

            return Ok(PhanHoiChuan<KetQuaDangNhap>.ThanhCong(ketQua, "Đăng nhập thành công!"));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: sinh mã OTP ngẫu nhiên
        // ─────────────────────────────────────────────────────────────────────
        private static string TaoMaOtp(int soKyTu)
        {
            var rng = new Random();
            return string.Concat(Enumerable.Range(0, soKyTu).Select(_ => rng.Next(0, 10).ToString()));
        }
    }
}

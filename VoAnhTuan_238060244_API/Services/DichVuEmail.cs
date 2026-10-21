using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace VoAnhTuan_238060244_API.Services
{
    public interface IDichVuEmail
    {
        Task GuiOtpDangNhapAsync(string emailNhan, string hoTen, string maOtp);
        Task GuiOtpXacThucEmailAsync(string emailNhan, string hoTen, string maOtp);
    }

    public class DichVuEmail : IDichVuEmail
    {
        private readonly IConfiguration _cauHinh;
        private readonly ILogger<DichVuEmail> _logger;

        public DichVuEmail(IConfiguration cauHinh, ILogger<DichVuEmail> logger)
        {
            _cauHinh = cauHinh;
            _logger  = logger;
        }

        private async Task GuiEmailAsync(string emailNhan, string hoTen, string tieuDe, string noiDungHtml)
        {
            var smtp = _cauHinh.GetSection("CaiDatSmtp");

            var tinNhan = new MimeMessage();
            tinNhan.From.Add(new MailboxAddress(smtp["TenNguoiGui"], smtp["TenDangNhap"]));
            tinNhan.To.Add(new MailboxAddress(hoTen, emailNhan));
            tinNhan.Subject = tieuDe;

            var builder = new BodyBuilder { HtmlBody = noiDungHtml };
            tinNhan.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp["MayChu"],
                int.Parse(smtp["Cong"] ?? "587"),
                bool.Parse(smtp["DungSsl"] ?? "true") ? SecureSocketOptions.StartTls : SecureSocketOptions.None
            );
            await client.AuthenticateAsync(smtp["TenDangNhap"], smtp["MatKhau"]);
            await client.SendAsync(tinNhan);
            await client.DisconnectAsync(true);
        }

        public async Task GuiOtpDangNhapAsync(string emailNhan, string hoTen, string maOtp)
        {
            var tieuDe = "🔐 Mã OTP đăng nhập hệ thống điểm danh";
            var noiDung = $"""
                <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #e0e0e0;border-radius:12px;overflow:hidden">
                  <div style="background:#1a73e8;padding:24px;text-align:center">
                    <h2 style="color:#fff;margin:0">Hệ Thống Điểm Danh Sinh Viên</h2>
                    <small style="color:#c9deff">VoAnhTuan_238060244</small>
                  </div>
                  <div style="padding:28px">
                    <p>Xin chào <strong>{hoTen}</strong>,</p>
                    <p>Mã OTP đăng nhập của bạn là:</p>
                    <div style="text-align:center;margin:24px 0">
                      <span style="font-size:42px;font-weight:bold;letter-spacing:10px;color:#1a73e8">{maOtp}</span>
                    </div>
                    <p style="color:#d32f2f;font-weight:bold">⏰ Mã có hiệu lực trong <u>5 phút</u>. Không chia sẻ mã này cho ai!</p>
                    <hr style="border:none;border-top:1px solid #eee"/>
                    <p style="color:#888;font-size:12px">Nếu bạn không thực hiện đăng nhập, hãy bỏ qua email này.</p>
                  </div>
                </div>
                """;

            try
            {
                await GuiEmailAsync(emailNhan, hoTen, tieuDe, noiDung);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi OTP đăng nhập đến {Email}", emailNhan);
                throw;
            }
        }

        public async Task GuiOtpXacThucEmailAsync(string emailNhan, string hoTen, string maOtp)
        {
            var tieuDe = "✅ Xác thực tài khoản - Hệ thống điểm danh";
            var noiDung = $"""
                <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #e0e0e0;border-radius:12px;overflow:hidden">
                  <div style="background:#34a853;padding:24px;text-align:center">
                    <h2 style="color:#fff;margin:0">Xác Thực Tài Khoản</h2>
                    <small style="color:#c8f0d4">VoAnhTuan_238060244</small>
                  </div>
                  <div style="padding:28px">
                    <p>Xin chào <strong>{hoTen}</strong>,</p>
                    <p>Cảm ơn bạn đã đăng ký! Mã xác thực email của bạn là:</p>
                    <div style="text-align:center;margin:24px 0">
                      <span style="font-size:42px;font-weight:bold;letter-spacing:10px;color:#34a853">{maOtp}</span>
                    </div>
                    <p style="color:#d32f2f;font-weight:bold">⏰ Mã có hiệu lực trong <u>15 phút</u>.</p>
                    <hr style="border:none;border-top:1px solid #eee"/>
                    <p style="color:#888;font-size:12px">Nếu bạn không đăng ký tài khoản này, hãy bỏ qua email.</p>
                  </div>
                </div>
                """;

            try
            {
                await GuiEmailAsync(emailNhan, hoTen, tieuDe, noiDung);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi OTP xác thực email đến {Email}", emailNhan);
                throw;
            }
        }
    }
}

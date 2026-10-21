using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Data;

namespace VoAnhTuan_238060244_API.Services
{
    /// <summary>
    /// Background service tự động làm mới QR token mỗi 30 giây
    /// cho tất cả buổi học đang mở điểm danh
    /// </summary>
    public class QrLamMoiDichVu : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QrLamMoiDichVu> _logger;
        private readonly IConfiguration _cauHinh;

        public QrLamMoiDichVu(
            IServiceScopeFactory scopeFactory,
            ILogger<QrLamMoiDichVu> logger,
            IConfiguration cauHinh)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
            _cauHinh      = cauHinh;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var giayDoi = int.Parse(
                _cauHinh["CaiDatDiemDanh:ThoiGianQrGiay"] ?? "30");

            _logger.LogInformation("QR làm mới service khởi động — chu kỳ {Giay}s", giayDoi);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(giayDoi), stoppingToken);

                try
                {
                    await LamMoiTatCaQrToken();
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Lỗi khi làm mới QR token");
                }
            }
        }

        private async Task LamMoiTatCaQrToken()
        {
            using var phamVi = _scopeFactory.CreateScope();
            var db = phamVi.ServiceProvider.GetRequiredService<AppDbContext>();

            var buoiDangMo = await db.Sessions
                .Where(s => s.DangMoDiemDanh)
                .ToListAsync();

            if (!buoiDangMo.Any()) return;

            foreach (var buoi in buoiDangMo)
            {
                buoi.QrToken          = SinhQrToken(buoi.SessionId);
                buoi.QrTokenCapNhatLuc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            _logger.LogDebug("Đã làm mới QR cho {SoBuoi} buổi học", buoiDangMo.Count);
        }

        /// <summary>
        /// Sinh QR token an toàn: sessionId + timestamp + entropy ngẫu nhiên → mã hóa Base64
        /// </summary>
        public static string SinhQrToken(int sessionId)
        {
            var khoaHmac = System.Text.Encoding.UTF8.GetBytes("VAT_QR_SECRET_238060244");
            var noiDung  = $"{sessionId}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{Guid.NewGuid():N}";
            var byteNoiDung = System.Text.Encoding.UTF8.GetBytes(noiDung);

            using var hmac = new System.Security.Cryptography.HMACSHA256(khoaHmac);
            var hash = hmac.ComputeHash(byteNoiDung);

            // Token = base64(sessionId) + "." + base64(hash[:16])
            var phan1 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sessionId.ToString()))
                               .Replace("=", "").Replace("+", "-").Replace("/", "_");
            var phan2 = Convert.ToBase64String(hash[..16])
                               .Replace("=", "").Replace("+", "-").Replace("/", "_");

            return $"{phan1}.{phan2}";
        }
    }
}

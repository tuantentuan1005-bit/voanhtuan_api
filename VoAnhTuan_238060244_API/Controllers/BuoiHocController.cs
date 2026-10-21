using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.DTOs;
using VoAnhTuan_238060244_API.Models;

namespace VoAnhTuan_238060244_API.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    [Produces("application/json")]
    public class BuoiHocController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BuoiHocController(AppDbContext db) => _db = db;

        // GET /api/sessions?Trang=1&SoLuong=10&TuKhoa=abc
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DanhSach([FromQuery] ThamSoPhanTrang thamSo, [FromQuery] int? classId)
        {
            var truy = _db.Sessions
                .Include(s => s.VatClass)
                    .ThenInclude(c => c!.VatCourse)
                .AsNoTracking();

            if (classId.HasValue)
                truy = truy.Where(s => s.ClassId == classId.Value);

            if (!string.IsNullOrWhiteSpace(thamSo.TuKhoa))
            {
                var kw = thamSo.TuKhoa.Trim().ToLower();
                truy = truy.Where(s => s.TieuDe.ToLower().Contains(kw));
            }

            var tong = await truy.CountAsync();
            var ds = await truy
                .OrderByDescending(s => s.ThoiGianBatDau)
                .Skip((thamSo.Trang - 1) * thamSo.SoLuong)
                .Take(thamSo.SoLuong)
                .Select(s => new
                {
                    s.SessionId, s.TieuDe,
                    s.ThoiGianBatDau, s.ThoiGianKetThuc,
                    s.DangMoDiemDanh, s.NgayTao,
                    LopHoc = s.VatClass != null
                        ? new { s.VatClass.ClassId, s.VatClass.TenLopHoc,
                                MonHoc = s.VatClass.VatCourse != null ? s.VatClass.VatCourse.TenMonHoc : "" }
                        : null
                })
                .ToListAsync();

            return Ok(PhanHoiChuan<KetQuaPhanTrang<object>>.ThanhCong(new KetQuaPhanTrang<object>
            {
                DuLieu           = ds.Cast<object>().ToList(),
                TrangHienTai     = thamSo.Trang,
                TongBanGhi       = tong,
                SoBanGhiMoiTrang = thamSo.SoLuong,
                TongTrang        = (int)Math.Ceiling((double)tong / thamSo.SoLuong)
            }));
        }

        // GET /api/sessions/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var s = await _db.Sessions
                .Include(s => s.VatClass).ThenInclude(c => c!.VatCourse)
                .Include(s => s.DanhSachDiemDanh).ThenInclude(a => a.VatUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (s == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy buổi học."));

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                s.SessionId, s.TieuDe,
                s.ThoiGianBatDau, s.ThoiGianKetThuc,
                s.DangMoDiemDanh, s.NgayTao,
                LopHoc = s.VatClass != null
                    ? new { s.VatClass.ClassId, s.VatClass.TenLopHoc }
                    : null,
                TongDiemDanh = s.DanhSachDiemDanh.Count,
                CoMat  = s.DanhSachDiemDanh.Count(a => a.TrangThai == "CóMặt"),
                VangMat = s.DanhSachDiemDanh.Count(a => a.TrangThai == "VắngMặt"),
                TreGio  = s.DanhSachDiemDanh.Count(a => a.TrangThai == "TrễGiờ")
            }));
        }

        // POST /api/sessions  (Admin, GiangVien)
        [HttpPost]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> TaoMoi([FromBody] YeuCauTaoBuoiHoc yc)
        {
            if (string.IsNullOrWhiteSpace(yc.TieuDe))
                return BadRequest(PhanHoiChuan.ThatBai("Tiêu đề buổi học không được để trống."));

            if (!await _db.Classes.AnyAsync(c => c.ClassId == yc.ClassId))
                return BadRequest(PhanHoiChuan.ThatBai("Lớp học không tồn tại."));

            if (yc.ThoiGianKetThuc <= yc.ThoiGianBatDau)
                return BadRequest(PhanHoiChuan.ThatBai("Thời gian kết thúc phải sau thời gian bắt đầu."));

            var buoiHoc = new VatSession
            {
                TieuDe          = yc.TieuDe.Trim(),
                ClassId         = yc.ClassId,
                ThoiGianBatDau  = yc.ThoiGianBatDau.ToUniversalTime(),
                ThoiGianKetThuc = yc.ThoiGianKetThuc.ToUniversalTime()
            };

            _db.Sessions.Add(buoiHoc);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ChiTiet), new { id = buoiHoc.SessionId },
                PhanHoiChuan.ThanhCong(new { buoiHoc.SessionId }, "Tạo buổi học thành công."));
        }

        // PUT /api/sessions/{id}  (Admin, GiangVien)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> CapNhat(int id, [FromBody] YeuCauCapNhatBuoiHoc yc)
        {
            var buoiHoc = await _db.Sessions.FindAsync(id);
            if (buoiHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy buổi học."));

            if (yc.ThoiGianKetThuc <= yc.ThoiGianBatDau)
                return BadRequest(PhanHoiChuan.ThatBai("Thời gian kết thúc phải sau thời gian bắt đầu."));

            buoiHoc.TieuDe          = yc.TieuDe.Trim();
            buoiHoc.ThoiGianBatDau  = yc.ThoiGianBatDau.ToUniversalTime();
            buoiHoc.ThoiGianKetThuc = yc.ThoiGianKetThuc.ToUniversalTime();

            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Cập nhật buổi học thành công."));
        }

        // DELETE /api/sessions/{id}  (Admin, GiangVien)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> Xoa(int id)
        {
            var buoiHoc = await _db.Sessions.FindAsync(id);
            if (buoiHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy buổi học."));

            if (buoiHoc.DangMoDiemDanh)
                return BadRequest(PhanHoiChuan.ThatBai("Không thể xóa buổi học đang mở điểm danh."));

            _db.Sessions.Remove(buoiHoc);
            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Xóa buổi học thành công."));
        }

        // POST /api/sessions/{id}/open-attendance  (Admin, GiangVien)
        [HttpPost("{id:int}/open-attendance")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> MoDiemDanh(int id)
        {
            var buoiHoc = await _db.Sessions
                .Include(s => s.VatClass)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (buoiHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy buổi học."));

            var tokenMoi = Services.QrLamMoiDichVu.SinhQrToken(buoiHoc.SessionId);
            buoiHoc.DangMoDiemDanh = true;
            buoiHoc.QrToken = tokenMoi;
            buoiHoc.QrTokenCapNhatLuc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan<KetQuaMoDiemDanh>.ThanhCong(new KetQuaMoDiemDanh
            {
                SessionId = buoiHoc.SessionId,
                QrToken = tokenMoi,
                QrCapNhatLuc = buoiHoc.QrTokenCapNhatLuc.Value,
                ChuKyDoiQrGiay = 30,
                ThongBao = "Đã mở điểm danh. Mã QR sẽ tự động thay đổi mỗi 30 giây."
            }, "Mở điểm danh thành công!"));
        }

        // POST /api/sessions/{id}/close-attendance  (Admin, GiangVien)
        [HttpPost("{id:int}/close-attendance")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> DongDiemDanh(int id)
        {
            var buoiHoc = await _db.Sessions.FindAsync(id);
            if (buoiHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy buổi học."));

            buoiHoc.DangMoDiemDanh = false;
            buoiHoc.QrToken = null;
            buoiHoc.QrTokenCapNhatLuc = null;

            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Đã đóng điểm danh cho buổi học này."));
        }

        // GET /api/sessions/{id}/current-qr  (Admin, GiangVien)
        [HttpGet("{id:int}/current-qr")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> LayQrHienTai(int id)
        {
            var buoiHoc = await _db.Sessions.FindAsync(id);
            if (buoiHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy buổi học."));

            if (!buoiHoc.DangMoDiemDanh || string.IsNullOrEmpty(buoiHoc.QrToken))
                return BadRequest(PhanHoiChuan.ThatBai("Buổi học chưa mở điểm danh hoặc đã đóng."));

            var thoiGianConLai = 30 - (DateTime.UtcNow - (buoiHoc.QrTokenCapNhatLuc ?? DateTime.UtcNow)).TotalSeconds;
            if (thoiGianConLai < 0) thoiGianConLai = 0;

            // Tạo QR Code PNG Base64 để hiển thị trực tiếp trên web/app
            string? qrBase64 = null;
            try
            {
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(buoiHoc.QrToken, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                var qrBytes = qrCode.GetGraphic(15);
                qrBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
            }
            catch
            {
                // Nếu không tạo được ảnh QR thì vẫn trả token dạng chuỗi
            }

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                buoiHoc.SessionId,
                buoiHoc.QrToken,
                buoiHoc.QrTokenCapNhatLuc,
                QrImageBase64 = qrBase64,
                ThoiGianConLaiGiay = Math.Round(thoiGianConLai, 1)
            }, "Lấy mã QR hiện tại thành công."));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.DTOs;
using VoAnhTuan_238060244_API.Helpers;
using VoAnhTuan_238060244_API.Models;

namespace VoAnhTuan_238060244_API.Controllers
{
    [ApiController]
    [Route("api/attendances")]
    [Produces("application/json")]
    public class DiemDanhController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DiemDanhController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ─── Lấy userId từ JWT claim ─────────────────────────────────────────
        private int LayUserIdHienTai()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        // GET /api/attendances?Trang=1&SoLuong=10&sessionId=1
        [HttpGet]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> DanhSach(
            [FromQuery] ThamSoPhanTrang thamSo,
            [FromQuery] int? sessionId,
            [FromQuery] int? userId)
        {
            var truy = _db.Attendances
                .Include(a => a.VatUser)
                .Include(a => a.VatSession).ThenInclude(s => s!.VatClass)
                .AsNoTracking();

            if (sessionId.HasValue) truy = truy.Where(a => a.SessionId == sessionId.Value);
            if (userId.HasValue)    truy = truy.Where(a => a.UserId    == userId.Value);

            if (!string.IsNullOrWhiteSpace(thamSo.TuKhoa))
            {
                var kw = thamSo.TuKhoa.Trim().ToLower();
                truy = truy.Where(a =>
                    (a.VatUser != null && a.VatUser.HoTen.ToLower().Contains(kw)) ||
                    (a.VatUser != null && a.VatUser.MaSoSinhVien != null &&
                     a.VatUser.MaSoSinhVien.ToLower().Contains(kw)));
            }

            var tong = await truy.CountAsync();
            var ds = await truy
                .OrderByDescending(a => a.NgayTao)
                .Skip((thamSo.Trang - 1) * thamSo.SoLuong)
                .Take(thamSo.SoLuong)
                .Select(a => new
                {
                    a.AttendanceId, a.TrangThai,
                    a.ThoiGianDiemDanh, a.NgayTao,
                    a.ViDoCheckIn, a.KinhDoCheckIn, a.KhoangCach,
                    a.DuongDanAnh, a.ThietBiDaDung,
                    SinhVien = a.VatUser != null
                        ? new { a.VatUser.UserId, a.VatUser.HoTen, a.VatUser.MaSoSinhVien }
                        : null,
                    BuoiHoc = a.VatSession != null
                        ? new { a.VatSession.SessionId, a.VatSession.TieuDe,
                                LopHoc = a.VatSession.VatClass != null ? a.VatSession.VatClass.TenLopHoc : "" }
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

        // GET /api/attendances/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var a = await _db.Attendances
                .Include(a => a.VatUser)
                .Include(a => a.VatSession).ThenInclude(s => s!.VatClass)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (a == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy bản ghi điểm danh."));

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                a.AttendanceId, a.TrangThai,
                a.ThoiGianDiemDanh, a.NgayTao,
                a.ViDoCheckIn, a.KinhDoCheckIn, a.KhoangCach,
                a.DuongDanAnh, a.ThietBiDaDung,
                SinhVien = a.VatUser != null
                    ? new { a.VatUser.UserId, a.VatUser.HoTen, a.VatUser.MaSoSinhVien, a.VatUser.Email }
                    : null,
                BuoiHoc = a.VatSession != null
                    ? new { a.VatSession.SessionId, a.VatSession.TieuDe,
                            a.VatSession.ThoiGianBatDau, a.VatSession.ThoiGianKetThuc }
                    : null
            }));
        }

        // PUT /api/attendances/{id}  — Giảng viên sửa trạng thái thủ công
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> CapNhatTrangThai(int id, [FromBody] YeuCauCapNhatDiemDanh yc)
        {
            var dsDiemDanh = await _db.Attendances.FindAsync(id);
            if (dsDiemDanh == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy bản ghi điểm danh."));

            var trangThaiHopLe = new[] { "CóMặt", "VắngMặt", "TrễGiờ" };
            if (!trangThaiHopLe.Contains(yc.TrangThai))
                return BadRequest(PhanHoiChuan.ThatBai("Trạng thái không hợp lệ. Chỉ chấp nhận: CóMặt, VắngMặt, TrễGiờ."));

            dsDiemDanh.TrangThai = yc.TrangThai;
            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan.ThanhCong(null, $"Đã cập nhật trạng thái thành '{yc.TrangThai}'."));
        }

        // DELETE /api/attendances/{id}  (Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Xoa(int id)
        {
            var diemDanh = await _db.Attendances.FindAsync(id);
            if (diemDanh == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy bản ghi điểm danh."));

            _db.Attendances.Remove(diemDanh);
            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan.ThanhCong(null, "Xóa bản ghi điểm danh thành công."));
        }

        // GET /api/attendances/my  — Sinh viên xem lịch sử điểm danh của mình
        [HttpGet("my")]
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> LichSuCuaToi([FromQuery] ThamSoPhanTrang thamSo)
        {
            var userId = LayUserIdHienTai();
            if (userId == 0)
                return Unauthorized(PhanHoiChuan.ThatBai("Không xác định được người dùng."));

            var truy = _db.Attendances
                .Where(a => a.UserId == userId)
                .Include(a => a.VatSession).ThenInclude(s => s!.VatClass).ThenInclude(c => c!.VatCourse)
                .AsNoTracking();

            var tong = await truy.CountAsync();
            var ds = await truy
                .OrderByDescending(a => a.ThoiGianDiemDanh)
                .Skip((thamSo.Trang - 1) * thamSo.SoLuong)
                .Take(thamSo.SoLuong)
                .Select(a => new
                {
                    a.AttendanceId, a.TrangThai, a.ThoiGianDiemDanh,
                    BuoiHoc = a.VatSession != null
                        ? new
                        {
                            a.VatSession.TieuDe,
                            a.VatSession.ThoiGianBatDau,
                            LopHoc  = a.VatSession.VatClass != null ? a.VatSession.VatClass.TenLopHoc : "",
                            MonHoc  = a.VatSession.VatClass != null && a.VatSession.VatClass.VatCourse != null
                                        ? a.VatSession.VatClass.VatCourse.TenMonHoc : ""
                        }
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

        // POST /api/attendances/check-in  (Sinh viên điểm danh)
        [HttpPost("check-in")]
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> CheckIn([FromBody] YeuCauCheckIn yc)
        {
            var userId = LayUserIdHienTai();
            if (userId == 0)
                return Unauthorized(PhanHoiChuan.ThatBai("Không xác định được danh tính sinh viên."));

            if (string.IsNullOrWhiteSpace(yc.QrToken))
                return BadRequest(PhanHoiChuan.ThatBai("Mã QR không được để trống."));

            if (string.IsNullOrWhiteSpace(yc.MaThietBi))
                return BadRequest(PhanHoiChuan.ThatBai("Mã thiết bị không được để trống."));

            // 1. Tìm buổi học tương ứng với QR token đang mở
            var buoiHoc = await _db.Sessions
                .Include(s => s.VatClass)
                .FirstOrDefaultAsync(s => s.QrToken == yc.QrToken.Trim() && s.DangMoDiemDanh);

            if (buoiHoc == null)
            {
                return BadRequest(PhanHoiChuan.ThatBai("Mã QR không hợp lệ hoặc buổi học chưa mở điểm danh."));
            }

            // 2. Kiểm tra hạn QR token (chu kỳ 30 giây + 10 giây độ trễ mạng)
            if (buoiHoc.QrTokenCapNhatLuc == null ||
                (DateTime.UtcNow - buoiHoc.QrTokenCapNhatLuc.Value).TotalSeconds > 40)
            {
                return BadRequest(PhanHoiChuan.ThatBai("Mã QR đã hết hạn (chỉ có hiệu lực trong 30 giây). Vui lòng quét lại mã mới trên màn hình!"));
            }

            // 3. Kiểm tra sinh viên có trong danh sách lớp học không
            var coTrongLop = await _db.Enrollments
                .AnyAsync(e => e.UserId == userId && e.ClassId == buoiHoc.ClassId && !e.DaXoa);

            if (!coTrongLop)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    PhanHoiChuan.ThatBai("Bạn chưa đăng ký lớp học này nên không thể điểm danh."));
            }

            // 4. Kiểm tra xem sinh viên đã điểm danh thành công trước đó chưa
            var diemDanhCu = await _db.Attendances
                .FirstOrDefaultAsync(a => a.SessionId == buoiHoc.SessionId && a.UserId == userId);

            if (diemDanhCu != null && (diemDanhCu.TrangThai == "CóMặt" || diemDanhCu.TrangThai == "TrễGiờ"))
            {
                return Conflict(PhanHoiChuan.ThatBai($"Bạn đã điểm danh buổi học này vào lúc {diemDanhCu.ThoiGianDiemDanh?.ToLocalTime():HH:mm:ss dd/MM/yyyy}."));
            }

            // 5. Kiểm tra GPS vị trí trong bán kính cho phép (nếu lớp có tọa độ)
            double? khoangCach = null;
            if (buoiHoc.VatClass?.ViDo != null && buoiHoc.VatClass?.KinhDo != null)
            {
                khoangCach = TinhKhoangCachGps.TinhMet(
                    buoiHoc.VatClass.ViDo.Value, buoiHoc.VatClass.KinhDo.Value,
                    yc.ViDo, yc.KinhDo);

                if (khoangCach > buoiHoc.VatClass.BanKinhChoPhep)
                {
                    return BadRequest(PhanHoiChuan.ThatBai(
                        $"Vị trí của bạn cách lớp học {khoangCach}m, vượt quá bán kính cho phép ({buoiHoc.VatClass.BanKinhChoPhep}m). Vui lòng đến gần phòng học hơn."));
                }
            }

            // 6. Kiểm tra thiết bị đã đăng ký
            var cacThietBi = await _db.Devices
                .Where(d => d.UserId == userId)
                .ToListAsync();

            var thietBi = cacThietBi.FirstOrDefault(d => d.MaThietBi == yc.MaThietBi.Trim());

            if (cacThietBi.Any())
            {
                if (thietBi == null)
                {
                    return BadRequest(PhanHoiChuan.ThatBai("Thiết bị này chưa được đăng ký! Mỗi tài khoản sinh viên chỉ được điểm danh trên thiết bị chính thức đã đăng ký."));
                }
                if (!thietBi.DuocPhepDiemDanh)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        PhanHoiChuan.ThatBai("Thiết bị của bạn đã bị vô hiệu hóa quyền điểm danh. Vui lòng liên hệ giảng viên."));
                }
                thietBi.LanDangNhapCuoi = DateTime.UtcNow;
            }
            else
            {
                // Tự động đăng ký thiết bị đầu tiên cho sinh viên
                thietBi = new VatDevice
                {
                    UserId = userId,
                    MaThietBi = yc.MaThietBi.Trim(),
                    TenThietBi = yc.TenThietBi?.Trim() ?? "Thiết bị di động",
                    HeDieuHanh = yc.HeDieuHanh?.Trim(),
                    DuocPhepDiemDanh = true,
                    NgayDangKy = DateTime.UtcNow,
                    LanDangNhapCuoi = DateTime.UtcNow
                };
                _db.Devices.Add(thietBi);
            }

            // 7. Xử lý lưu ảnh khuôn mặt nếu có
            string? duongDanAnh = null;
            if (!string.IsNullOrWhiteSpace(yc.AnhKhuonMatBase64))
            {
                try
                {
                    var base64Data = yc.AnhKhuonMatBase64;
                    if (base64Data.Contains(","))
                        base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);

                    var bytes = Convert.FromBase64String(base64Data);
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var thuMucUpload = Path.Combine(webRoot, "uploads", "attendances");
                    if (!Directory.Exists(thuMucUpload))
                        Directory.CreateDirectory(thuMucUpload);

                    var tenFile = $"face_{userId}_{buoiHoc.SessionId}_{DateTime.UtcNow.Ticks}.jpg";
                    var duongDanDayDu = Path.Combine(thuMucUpload, tenFile);
                    await System.IO.File.WriteAllBytesAsync(duongDanDayDu, bytes);

                    duongDanAnh = $"/uploads/attendances/{tenFile}";
                }
                catch
                {
                    // Lỗi lưu ảnh không gián đoạn việc ghi nhận điểm danh
                }
            }

            // 8. Đánh giá trạng thái CóMặt hay TrễGiờ (trễ sau 15 phút đầu giờ)
            var trangThai = (DateTime.UtcNow > buoiHoc.ThoiGianBatDau.AddMinutes(15)) ? "TrễGiờ" : "CóMặt";

            if (diemDanhCu != null)
            {
                diemDanhCu.TrangThai = trangThai;
                diemDanhCu.ThoiGianDiemDanh = DateTime.UtcNow;
                diemDanhCu.ViDoCheckIn = yc.ViDo;
                diemDanhCu.KinhDoCheckIn = yc.KinhDo;
                diemDanhCu.KhoangCach = khoangCach;
                diemDanhCu.DuongDanAnh = duongDanAnh ?? diemDanhCu.DuongDanAnh;
                diemDanhCu.ThietBiDaDung = yc.MaThietBi;
            }
            else
            {
                var diemDanhMoi = new VatAttendance
                {
                    SessionId = buoiHoc.SessionId,
                    UserId = userId,
                    TrangThai = trangThai,
                    ThoiGianDiemDanh = DateTime.UtcNow,
                    ViDoCheckIn = yc.ViDo,
                    KinhDoCheckIn = yc.KinhDo,
                    KhoangCach = khoangCach,
                    DuongDanAnh = duongDanAnh,
                    ThietBiDaDung = yc.MaThietBi,
                    NgayTao = DateTime.UtcNow
                };
                _db.Attendances.Add(diemDanhMoi);
            }

            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                buoiHoc.SessionId,
                buoiHoc.TieuDe,
                TrangThai = trangThai,
                ThoiGianDiemDanh = DateTime.UtcNow,
                KhoangCachMet = khoangCach,
                AnhKhuonMat = duongDanAnh,
                ThietBi = yc.MaThietBi
            }, $"Điểm danh thành công! Trạng thái: {trangThai}."));
        }

        // POST /api/attendances/register-device  (Đăng ký thiết bị cho sinh viên)
        [HttpPost("register-device")]
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> DangKyThietBi([FromBody] YeuCauDangKyThietBi yc)
        {
            var userId = LayUserIdHienTai();
            if (userId == 0)
                return Unauthorized(PhanHoiChuan.ThatBai("Không xác định được danh tính."));

            if (string.IsNullOrWhiteSpace(yc.MaThietBi))
                return BadRequest(PhanHoiChuan.ThatBai("Mã thiết bị không được để trống."));

            var tonTai = await _db.Devices.FirstOrDefaultAsync(d => d.UserId == userId && d.MaThietBi == yc.MaThietBi.Trim());
            if (tonTai != null)
            {
                tonTai.TenThietBi = yc.TenThietBi?.Trim() ?? tonTai.TenThietBi;
                tonTai.HeDieuHanh = yc.HeDieuHanh?.Trim() ?? tonTai.HeDieuHanh;
                tonTai.LanDangNhapCuoi = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(PhanHoiChuan.ThanhCong(tonTai, "Thiết bị đã tồn tại và được cập nhật."));
            }

            var tbMoi = new VatDevice
            {
                UserId = userId,
                MaThietBi = yc.MaThietBi.Trim(),
                TenThietBi = yc.TenThietBi?.Trim() ?? "Thiết bị di động",
                HeDieuHanh = yc.HeDieuHanh?.Trim(),
                DuocPhepDiemDanh = true,
                NgayDangKy = DateTime.UtcNow,
                LanDangNhapCuoi = DateTime.UtcNow
            };
            _db.Devices.Add(tbMoi);
            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan.ThanhCong(tbMoi, "Đăng ký thiết bị thành công."));
        }
    }
}

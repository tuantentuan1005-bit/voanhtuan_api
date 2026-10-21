using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.DTOs;
using VoAnhTuan_238060244_API.Models;

namespace VoAnhTuan_238060244_API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class NguoiDungController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NguoiDungController(AppDbContext db) => _db = db;

        // GET /api/users?Trang=1&SoLuong=10&TuKhoa=abc
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] ThamSoPhanTrang thamSo)
        {
            var truy = _db.Users
                .Include(u => u.VatRole)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(thamSo.TuKhoa))
            {
                var kw = thamSo.TuKhoa.Trim().ToLower();
                truy = truy.Where(u =>
                    u.HoTen.ToLower().Contains(kw) ||
                    u.Email.ToLower().Contains(kw) ||
                    u.TenDangNhap.ToLower().Contains(kw) ||
                    (u.MaSoSinhVien != null && u.MaSoSinhVien.ToLower().Contains(kw)));
            }

            var tong = await truy.CountAsync();
            var ds = await truy
                .OrderBy(u => u.UserId)
                .Skip((thamSo.Trang - 1) * thamSo.SoLuong)
                .Take(thamSo.SoLuong)
                .Select(u => new
                {
                    u.UserId, u.HoTen, u.Email, u.TenDangNhap,
                    u.SoDienThoai, u.MaSoSinhVien, u.KichHoat,
                    u.EmailDaXacThuc, u.NgayTao,
                    VaiTro = u.VatRole != null ? u.VatRole.TenVaiTro : ""
                })
                .ToListAsync();

            var phanTrang = new KetQuaPhanTrang<object>
            {
                DuLieu           = ds.Cast<object>().ToList(),
                TrangHienTai     = thamSo.Trang,
                TongBanGhi       = tong,
                SoBanGhiMoiTrang = thamSo.SoLuong,
                TongTrang        = (int)Math.Ceiling((double)tong / thamSo.SoLuong)
            };

            return Ok(PhanHoiChuan<KetQuaPhanTrang<object>>.ThanhCong(phanTrang));
        }

        // GET /api/users/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var u = await _db.Users
                .Include(u => u.VatRole)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (u == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy người dùng."));

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                u.UserId, u.HoTen, u.Email, u.TenDangNhap,
                u.SoDienThoai, u.MaSoSinhVien, u.KichHoat,
                u.EmailDaXacThuc, u.NgayTao,
                VaiTro   = u.VatRole?.TenVaiTro,
                u.RoleId
            }));
        }

        // POST /api/users
        [HttpPost]
        public async Task<IActionResult> TaoMoi([FromBody] YeuCauTaoNguoiDung yc)
        {
            if (string.IsNullOrWhiteSpace(yc.HoTen) || string.IsNullOrWhiteSpace(yc.Email) ||
                string.IsNullOrWhiteSpace(yc.TenDangNhap) || string.IsNullOrWhiteSpace(yc.MatKhau))
                return BadRequest(PhanHoiChuan.ThatBai("Vui lòng điền đầy đủ thông tin."));

            if (await _db.Users.AnyAsync(u => u.Email == yc.Email.Trim().ToLower()))
                return Conflict(PhanHoiChuan.ThatBai("Email đã tồn tại."));

            if (await _db.Users.AnyAsync(u => u.TenDangNhap == yc.TenDangNhap.Trim()))
                return Conflict(PhanHoiChuan.ThatBai("Tên đăng nhập đã tồn tại."));

            if (!await _db.Roles.AnyAsync(r => r.RoleId == yc.RoleId))
                return BadRequest(PhanHoiChuan.ThatBai("Vai trò không hợp lệ."));

            var nguoiDung = new VatUser
            {
                HoTen          = yc.HoTen.Trim(),
                Email          = yc.Email.Trim().ToLower(),
                TenDangNhap    = yc.TenDangNhap.Trim(),
                MatKhauMaHoa   = BCrypt.Net.BCrypt.HashPassword(yc.MatKhau),
                SoDienThoai    = yc.SoDienThoai?.Trim(),
                MaSoSinhVien   = yc.MaSoSinhVien?.Trim(),
                RoleId         = yc.RoleId,
                EmailDaXacThuc = true, // Admin tạo thì không cần xác thực email
                KichHoat       = true
            };

            _db.Users.Add(nguoiDung);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ChiTiet), new { id = nguoiDung.UserId },
                PhanHoiChuan.ThanhCong(new { nguoiDung.UserId }, "Tạo người dùng thành công."));
        }

        // PUT /api/users/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> CapNhat(int id, [FromBody] YeuCauCapNhatNguoiDung yc)
        {
            var nguoiDung = await _db.Users.FindAsync(id);
            if (nguoiDung == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy người dùng."));

            if (!await _db.Roles.AnyAsync(r => r.RoleId == yc.RoleId))
                return BadRequest(PhanHoiChuan.ThatBai("Vai trò không hợp lệ."));

            // Kiểm tra email trùng với người khác
            if (await _db.Users.AnyAsync(u => u.Email == yc.Email.Trim().ToLower() && u.UserId != id))
                return Conflict(PhanHoiChuan.ThatBai("Email đã được dùng bởi tài khoản khác."));

            nguoiDung.HoTen        = yc.HoTen.Trim();
            nguoiDung.Email        = yc.Email.Trim().ToLower();
            nguoiDung.SoDienThoai  = yc.SoDienThoai?.Trim();
            nguoiDung.MaSoSinhVien = yc.MaSoSinhVien?.Trim();
            nguoiDung.RoleId       = yc.RoleId;
            nguoiDung.KichHoat     = yc.KichHoat;

            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Cập nhật người dùng thành công."));
        }

        // DELETE /api/users/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Xoa(int id)
        {
            var nguoiDung = await _db.Users.FindAsync(id);
            if (nguoiDung == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy người dùng."));

            // Khóa thay vì xóa hẳn để bảo toàn dữ liệu lịch sử
            nguoiDung.KichHoat = false;
            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan.ThanhCong(null, "Đã khóa tài khoản người dùng."));
        }

        // GET /api/users/roles
        [HttpGet("roles")]
        public async Task<IActionResult> DanhSachVaiTro()
        {
            var roles = await _db.Roles.AsNoTracking()
                .Select(r => new { r.RoleId, r.TenVaiTro })
                .ToListAsync();
            return Ok(PhanHoiChuan.ThanhCong(roles));
        }
    }
}

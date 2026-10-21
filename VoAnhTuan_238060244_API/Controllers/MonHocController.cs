using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.DTOs;
using VoAnhTuan_238060244_API.Models;

namespace VoAnhTuan_238060244_API.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Produces("application/json")]
    public class MonHocController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MonHocController(AppDbContext db) => _db = db;

        // GET /api/courses?Trang=1&SoLuong=10&TuKhoa=abc
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DanhSach([FromQuery] ThamSoPhanTrang thamSo)
        {
            var truy = _db.Courses.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(thamSo.TuKhoa))
            {
                var kw = thamSo.TuKhoa.Trim().ToLower();
                truy = truy.Where(c =>
                    c.TenMonHoc.ToLower().Contains(kw) ||
                    c.MaMonHoc.ToLower().Contains(kw) ||
                    (c.MoTa != null && c.MoTa.ToLower().Contains(kw)));
            }

            var tong = await truy.CountAsync();
            var ds = await truy
                .OrderBy(c => c.MaMonHoc)
                .Skip((thamSo.Trang - 1) * thamSo.SoLuong)
                .Take(thamSo.SoLuong)
                .Select(c => new
                {
                    c.CourseId, c.MaMonHoc, c.TenMonHoc,
                    c.SoTinChi, c.MoTa, c.CongKhai, c.NgayTao
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

        // GET /api/courses/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var c = await _db.Courses
                .AsNoTracking()
                .Include(c => c.DanhSachLopHoc)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (c == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy môn học."));

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                c.CourseId, c.MaMonHoc, c.TenMonHoc,
                c.SoTinChi, c.MoTa, c.CongKhai, c.NgayTao,
                SoLopHoc = c.DanhSachLopHoc.Count
            }));
        }

        // POST /api/courses  (Admin, GiangVien)
        [HttpPost]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> TaoMoi([FromBody] YeuCauTaoMonHoc yc)
        {
            if (string.IsNullOrWhiteSpace(yc.MaMonHoc) || string.IsNullOrWhiteSpace(yc.TenMonHoc))
                return BadRequest(PhanHoiChuan.ThatBai("Mã và tên môn học không được để trống."));

            if (await _db.Courses.AnyAsync(c => c.MaMonHoc == yc.MaMonHoc.Trim().ToUpper()))
                return Conflict(PhanHoiChuan.ThatBai("Mã môn học đã tồn tại."));

            var monHoc = new VatCourse
            {
                MaMonHoc  = yc.MaMonHoc.Trim().ToUpper(),
                TenMonHoc = yc.TenMonHoc.Trim(),
                SoTinChi  = yc.SoTinChi,
                MoTa      = yc.MoTa?.Trim(),
                CongKhai  = yc.CongKhai
            };

            _db.Courses.Add(monHoc);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ChiTiet), new { id = monHoc.CourseId },
                PhanHoiChuan.ThanhCong(new { monHoc.CourseId }, "Tạo môn học thành công."));
        }

        // PUT /api/courses/{id}  (Admin, GiangVien)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> CapNhat(int id, [FromBody] YeuCauCapNhatMonHoc yc)
        {
            var monHoc = await _db.Courses.FindAsync(id);
            if (monHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy môn học."));

            monHoc.TenMonHoc = yc.TenMonHoc.Trim();
            monHoc.SoTinChi  = yc.SoTinChi;
            monHoc.MoTa      = yc.MoTa?.Trim();
            monHoc.CongKhai  = yc.CongKhai;

            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Cập nhật môn học thành công."));
        }

        // DELETE /api/courses/{id}  (Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Xoa(int id)
        {
            var monHoc = await _db.Courses.FindAsync(id);
            if (monHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy môn học."));

            var coLopHoc = await _db.Classes.AnyAsync(c => c.CourseId == id);
            if (coLopHoc)
                return BadRequest(PhanHoiChuan.ThatBai("Không thể xóa môn học đang có lớp học liên kết."));

            _db.Courses.Remove(monHoc);
            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Xóa môn học thành công."));
        }
    }
}

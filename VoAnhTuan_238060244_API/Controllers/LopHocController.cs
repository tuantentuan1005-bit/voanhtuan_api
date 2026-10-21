using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.DTOs;
using VoAnhTuan_238060244_API.Models;

namespace VoAnhTuan_238060244_API.Controllers
{
    [ApiController]
    [Route("api/classes")]
    [Produces("application/json")]
    public class LopHocController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LopHocController(AppDbContext db) => _db = db;

        // GET /api/classes?Trang=1&SoLuong=10&TuKhoa=abc
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DanhSach([FromQuery] ThamSoPhanTrang thamSo)
        {
            var truy = _db.Classes
                .Include(c => c.VatCourse)
                .Include(c => c.GiangVien)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(thamSo.TuKhoa))
            {
                var kw = thamSo.TuKhoa.Trim().ToLower();
                truy = truy.Where(c =>
                    c.TenLopHoc.ToLower().Contains(kw) ||
                    c.MaLopHoc.ToLower().Contains(kw) ||
                    (c.VatCourse != null && c.VatCourse.TenMonHoc.ToLower().Contains(kw)) ||
                    (c.GiangVien != null && c.GiangVien.HoTen.ToLower().Contains(kw)));
            }

            var tong = await truy.CountAsync();
            var ds = await truy
                .OrderByDescending(c => c.NgayTao)
                .Skip((thamSo.Trang - 1) * thamSo.SoLuong)
                .Take(thamSo.SoLuong)
                .Select(c => new
                {
                    c.ClassId, c.MaLopHoc, c.TenLopHoc,
                    c.NamHoc, c.HocKy, c.DangHoatDong, c.NgayTao,
                    c.ViDo, c.KinhDo, c.BanKinhChoPhep,
                    MonHoc    = c.VatCourse != null ? new { c.VatCourse.CourseId, c.VatCourse.TenMonHoc } : null,
                    GiangVien = c.GiangVien != null ? new { c.GiangVien.UserId, c.GiangVien.HoTen } : null
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

        // GET /api/classes/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var c = await _db.Classes
                .Include(c => c.VatCourse)
                .Include(c => c.GiangVien)
                .Include(c => c.DanhSachDangKy.Where(e => !e.DaXoa))
                    .ThenInclude(e => e.VatUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (c == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy lớp học."));

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                c.ClassId, c.MaLopHoc, c.TenLopHoc,
                c.NamHoc, c.HocKy, c.DangHoatDong, c.NgayTao,
                c.ViDo, c.KinhDo, c.BanKinhChoPhep,
                MonHoc    = c.VatCourse != null ? new { c.VatCourse.CourseId, c.VatCourse.MaMonHoc, c.VatCourse.TenMonHoc } : null,
                GiangVien = c.GiangVien != null ? new { c.GiangVien.UserId, c.GiangVien.HoTen, c.GiangVien.Email } : null,
                SinhVien  = c.DanhSachDangKy.Select(e => new
                {
                    e.EnrollmentId,
                    e.VatUser!.UserId,
                    e.VatUser.HoTen,
                    e.VatUser.MaSoSinhVien,
                    e.NgayDangKy
                })
            }));
        }

        // POST /api/classes  (Admin, GiangVien)
        [HttpPost]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> TaoMoi([FromBody] YeuCauTaoLopHoc yc)
        {
            if (string.IsNullOrWhiteSpace(yc.MaLopHoc) || string.IsNullOrWhiteSpace(yc.TenLopHoc))
                return BadRequest(PhanHoiChuan.ThatBai("Mã và tên lớp học không được để trống."));

            if (!await _db.Courses.AnyAsync(c => c.CourseId == yc.CourseId))
                return BadRequest(PhanHoiChuan.ThatBai("Môn học không tồn tại."));

            if (!await _db.Users.AnyAsync(u => u.UserId == yc.GiangVienId))
                return BadRequest(PhanHoiChuan.ThatBai("Giảng viên không tồn tại."));

            if (await _db.Classes.AnyAsync(c => c.MaLopHoc == yc.MaLopHoc.Trim()))
                return Conflict(PhanHoiChuan.ThatBai("Mã lớp học đã tồn tại."));

            var lopHoc = new VatClass
            {
                MaLopHoc       = yc.MaLopHoc.Trim(),
                TenLopHoc      = yc.TenLopHoc.Trim(),
                CourseId       = yc.CourseId,
                GiangVienId    = yc.GiangVienId,
                NamHoc         = yc.NamHoc,
                HocKy          = yc.HocKy?.Trim(),
                ViDo           = yc.ViDo,
                KinhDo         = yc.KinhDo,
                BanKinhChoPhep = yc.BanKinhChoPhep
            };

            _db.Classes.Add(lopHoc);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ChiTiet), new { id = lopHoc.ClassId },
                PhanHoiChuan.ThanhCong(new { lopHoc.ClassId }, "Tạo lớp học thành công."));
        }

        // PUT /api/classes/{id}  (Admin, GiangVien)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> CapNhat(int id, [FromBody] YeuCauCapNhatLopHoc yc)
        {
            var lopHoc = await _db.Classes.FindAsync(id);
            if (lopHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy lớp học."));

            if (!await _db.Users.AnyAsync(u => u.UserId == yc.GiangVienId))
                return BadRequest(PhanHoiChuan.ThatBai("Giảng viên không tồn tại."));

            lopHoc.TenLopHoc      = yc.TenLopHoc.Trim();
            lopHoc.GiangVienId    = yc.GiangVienId;
            lopHoc.NamHoc         = yc.NamHoc;
            lopHoc.HocKy          = yc.HocKy?.Trim();
            lopHoc.ViDo           = yc.ViDo;
            lopHoc.KinhDo         = yc.KinhDo;
            lopHoc.BanKinhChoPhep = yc.BanKinhChoPhep;
            lopHoc.DangHoatDong   = yc.DangHoatDong;

            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Cập nhật lớp học thành công."));
        }

        // DELETE /api/classes/{id}  (Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Xoa(int id)
        {
            var lopHoc = await _db.Classes.FindAsync(id);
            if (lopHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy lớp học."));

            lopHoc.DangHoatDong = false;
            await _db.SaveChangesAsync();
            return Ok(PhanHoiChuan.ThanhCong(null, "Đã đóng lớp học."));
        }

        // POST /api/classes/{id}/enroll  — Đăng ký sinh viên vào lớp
        [HttpPost("{id:int}/enroll")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> DangKySinhVien(int id, [FromBody] int userId)
        {
            var lopHoc = await _db.Classes.FindAsync(id);
            if (lopHoc == null)
                return NotFound(PhanHoiChuan.ThatBai("Không tìm thấy lớp học."));

            if (!await _db.Users.AnyAsync(u => u.UserId == userId))
                return NotFound(PhanHoiChuan.ThatBai("Sinh viên không tồn tại."));

            var daDangKy = await _db.Enrollments
                .AnyAsync(e => e.UserId == userId && e.ClassId == id && !e.DaXoa);
            if (daDangKy)
                return Conflict(PhanHoiChuan.ThatBai("Sinh viên đã đăng ký lớp này rồi."));

            _db.Enrollments.Add(new VatEnrollment { UserId = userId, ClassId = id });
            await _db.SaveChangesAsync();

            return Ok(PhanHoiChuan.ThanhCong(null, "Đăng ký sinh viên vào lớp thành công."));
        }
    }
}

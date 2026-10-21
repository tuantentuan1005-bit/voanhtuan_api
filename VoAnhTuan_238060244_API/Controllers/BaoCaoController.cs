using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.DTOs;

namespace VoAnhTuan_238060244_API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Produces("application/json")]
    public class BaoCaoController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cauHinh;

        public BaoCaoController(AppDbContext db, IConfiguration cauHinh)
        {
            _db = db;
            _cauHinh = cauHinh;
        }

        // GET /api/reports/absence-warning
        // Danh sách sinh viên vắng nhiều, cảnh báo nguy cơ hoặc đã vượt ngưỡng cấm thi
        [HttpGet("absence-warning")]
        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> CanhBaoCamThi(
            [FromQuery] int? classId,
            [FromQuery] double? nguongCamThi)
        {
            // Ngưỡng cấm thi mặc định là 20% (0.2) theo quy chế học vụ
            var nguong = nguongCamThi ?? double.Parse(_cauHinh["CaiDatDiemDanh:NguongVangCanhBao"] ?? "0.2");

            // Lấy danh sách lớp học cần kiểm tra
            var truyLop = _db.Classes
                .Include(c => c.VatCourse)
                .Include(c => c.GiangVien)
                .Include(c => c.DanhSachBuoiHoc)
                .Include(c => c.DanhSachDangKy.Where(e => !e.DaXoa))
                    .ThenInclude(e => e.VatUser)
                .AsNoTracking();

            if (classId.HasValue)
            {
                truyLop = truyLop.Where(c => c.ClassId == classId.Value);
            }

            var danhSachLop = await truyLop.ToListAsync();
            var ketQuaCanhBao = new List<object>();

            foreach (var lop in danhSachLop)
            {
                var tongSoBuoi = lop.DanhSachBuoiHoc.Count;
                if (tongSoBuoi == 0) continue;

                var sessionIds = lop.DanhSachBuoiHoc.Select(s => s.SessionId).ToList();

                // Lấy tất cả bản ghi điểm danh thuộc các buổi của lớp này
                var tatCaDiemDanh = await _db.Attendances
                    .Where(a => sessionIds.Contains(a.SessionId))
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var enroll in lop.DanhSachDangKy)
                {
                    var sv = enroll.VatUser;
                    if (sv == null) continue;

                    // Các bản ghi điểm danh của sinh viên này trong lớp
                    var diemDanhCuaSv = tatCaDiemDanh.Where(a => a.UserId == sv.UserId).ToList();

                    var soBuoiCoMat = diemDanhCuaSv.Count(a => a.TrangThai == "CóMặt");
                    var soBuoiTre   = diemDanhCuaSv.Count(a => a.TrangThai == "TrễGiờ");
                    
                    // Số buổi vắng = Tổng buổi - CóMặt - TrễGiờ
                    // (hoặc những buổi được đánh dấu VắngMặt rõ ràng)
                    var soBuoiVang = tongSoBuoi - (soBuoiCoMat + soBuoiTre);
                    if (soBuoiVang < 0) soBuoiVang = 0;

                    var tyLeVang = (double)soBuoiVang / tongSoBuoi;

                    // Nếu tỷ lệ vắng >= 15% (cận kề hoặc vượt ngưỡng 20%)
                    if (tyLeVang >= (nguong * 0.75))
                    {
                        string mucDoCanhBao;
                        string thongBaoChiTiet;

                        if (tyLeVang >= nguong)
                        {
                            mucDoCanhBao = "CẤM THI";
                            thongBaoChiTiet = $"Đã vắng {soBuoiVang}/{tongSoBuoi} buổi ({tyLeVang * 100:0.#}%), vượt ngưỡng quy định {nguong * 100}%.";
                        }
                        else
                        {
                            mucDoCanhBao = "CẢNH BÁO";
                            thongBaoChiTiet = $"Đã vắng {soBuoiVang}/{tongSoBuoi} buổi ({tyLeVang * 100:0.#}%), sắp chạm ngưỡng cấm thi {nguong * 100}%.";
                        }

                        ketQuaCanhBao.Add(new
                        {
                            LopHoc = new
                            {
                                lop.ClassId,
                                lop.MaLopHoc,
                                lop.TenLopHoc,
                                MonHoc = lop.VatCourse?.TenMonHoc,
                                GiangVien = lop.GiangVien?.HoTen
                            },
                            SinhVien = new
                            {
                                sv.UserId,
                                sv.MaSoSinhVien,
                                sv.HoTen,
                                sv.Email,
                                sv.SoDienThoai
                            },
                            ThongKe = new
                            {
                                TongSoBuoi = tongSoBuoi,
                                SoBuoiCoMat = soBuoiCoMat,
                                SoBuoiTre = soBuoiTre,
                                SoBuoiVang = soBuoiVang,
                                TyLeVangPhanTram = Math.Round(tyLeVang * 100, 1),
                                NguongQuyDinhPhanTram = Math.Round(nguong * 100, 1)
                            },
                            MucDoCanhBao = mucDoCanhBao,
                            ThongBao = thongBaoChiTiet
                        });
                    }
                }
            }

            return Ok(PhanHoiChuan.ThanhCong(new
            {
                TongSoSinhVienBiCanhBao = ketQuaCanhBao.Count,
                NguongApDungPhanTram = Math.Round(nguong * 100, 1),
                DanhSachCanhBao = ketQuaCanhBao
            }, "Lấy báo cáo cảnh báo cấm thi thành công."));
        }
    }
}

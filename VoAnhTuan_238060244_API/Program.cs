using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VoAnhTuan_238060244_API.Data;
using VoAnhTuan_238060244_API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(tuyChon =>
    tuyChon.UseSqlServer(builder.Configuration.GetConnectionString("KetNoiChinh")));

// ── Services DI ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IDichVuJwt,   DichVuJwt>();
builder.Services.AddScoped<IDichVuEmail, DichVuEmail>();
builder.Services.AddHostedService<QrLamMoiDichVu>();

// ── Authentication JWT ────────────────────────────────────────────────────────
var cauHinhJwt  = builder.Configuration.GetSection("CaiDatJwt");
var khoaBiMat   = Encoding.UTF8.GetBytes(cauHinhJwt["BiMat"]!);

builder.Services.AddAuthentication(tuyChon =>
{
    tuyChon.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    tuyChon.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(tuyChon =>
{
    tuyChon.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(khoaBiMat),
        ValidateIssuer           = true,
        ValidIssuer              = cauHinhJwt["NhaPhatHanh"],
        ValidateAudience         = true,
        ValidAudience            = cauHinhJwt["DoiTuong"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(tuyChon =>
{
    tuyChon.AddPolicy("ChinhSachCors", chinh =>
        chinh.AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader());
});

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(cauHinh =>
{
    cauHinh.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "VoAnhTuan_238060244 - API Điểm Danh Sinh Viên",
        Version     = "v1",
        Description = "Tác giả: Võ Anh Tuấn | MSSV: 238060244"
    });

    cauHinh.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Nhập: Bearer {token}"
    });
    cauHinh.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VoAnhTuan_238060244 API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("ChinhSachCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto Migrate + Seed ───────────────────────────────────────────────────────
using (var phamVi = app.Services.CreateScope())
{
    var db = phamVi.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await SeedDuLieuMau(db);
}

app.Run();

// ════════════════════════════════════════════════════════════════════════════
// SEED DỮ LIỆU MẪU
// ════════════════════════════════════════════════════════════════════════════
static async Task SeedDuLieuMau(AppDbContext db)
{
    // Seed Roles
    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new VoAnhTuan_238060244_API.Models.VatRole { TenVaiTro = "Admin" },
            new VoAnhTuan_238060244_API.Models.VatRole { TenVaiTro = "GiangVien" },
            new VoAnhTuan_238060244_API.Models.VatRole { TenVaiTro = "SinhVien" }
        );
        await db.SaveChangesAsync();
    }

    var roleAdmin    = db.Roles.First(r => r.TenVaiTro == "Admin");
    var roleGV       = db.Roles.First(r => r.TenVaiTro == "GiangVien");
    var roleSV       = db.Roles.First(r => r.TenVaiTro == "SinhVien");

    // Seed Users
    if (!db.Users.Any())
    {
        db.Users.AddRange(
            // Admin
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Quản Trị Viên", Email = "admin@diemdanh.edu.vn",
                TenDangNhap = "admin", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = roleAdmin.RoleId, EmailDaXacThuc = true, KichHoat = true
            },
            // Giảng viên 1
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Nguyễn Văn Hùng", Email = "gv.hung@diemdanh.edu.vn",
                TenDangNhap = "gv_hung", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("GiangVien@123"),
                RoleId = roleGV.RoleId, EmailDaXacThuc = true, KichHoat = true
            },
            // Giảng viên 2
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Trần Thị Lan", Email = "gv.lan@diemdanh.edu.vn",
                TenDangNhap = "gv_lan", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("GiangVien@123"),
                RoleId = roleGV.RoleId, EmailDaXacThuc = true, KichHoat = true
            },
            // 5 Sinh viên
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Võ Anh Tuấn", Email = "sv.tuan@sinh.vien.edu.vn",
                TenDangNhap = "sv_tuan", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("SinhVien@123"),
                RoleId = roleSV.RoleId, MaSoSinhVien = "238060244",
                EmailDaXacThuc = true, KichHoat = true
            },
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Lê Thị Mai", Email = "sv.mai@sinh.vien.edu.vn",
                TenDangNhap = "sv_mai", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("SinhVien@123"),
                RoleId = roleSV.RoleId, MaSoSinhVien = "238060001",
                EmailDaXacThuc = true, KichHoat = true
            },
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Phạm Quốc Bảo", Email = "sv.bao@sinh.vien.edu.vn",
                TenDangNhap = "sv_bao", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("SinhVien@123"),
                RoleId = roleSV.RoleId, MaSoSinhVien = "238060002",
                EmailDaXacThuc = true, KichHoat = true
            },
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Nguyễn Minh Khôi", Email = "sv.khoi@sinh.vien.edu.vn",
                TenDangNhap = "sv_khoi", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("SinhVien@123"),
                RoleId = roleSV.RoleId, MaSoSinhVien = "238060003",
                EmailDaXacThuc = true, KichHoat = true
            },
            new VoAnhTuan_238060244_API.Models.VatUser
            {
                HoTen = "Đặng Thị Hoa", Email = "sv.hoa@sinh.vien.edu.vn",
                TenDangNhap = "sv_hoa", MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword("SinhVien@123"),
                RoleId = roleSV.RoleId, MaSoSinhVien = "238060004",
                EmailDaXacThuc = true, KichHoat = true
            }
        );
        await db.SaveChangesAsync();

        // Seed Courses
        db.Courses.AddRange(
            new VoAnhTuan_238060244_API.Models.VatCourse
            {
                MaMonHoc = "LTDDDN", TenMonHoc = "Lập trình ứng dụng di động",
                SoTinChi = 3, MoTa = "Môn học xây dựng ứng dụng di động với Flutter/React Native"
            },
            new VoAnhTuan_238060244_API.Models.VatCourse
            {
                MaMonHoc = "CSDL", TenMonHoc = "Cơ sở dữ liệu nâng cao",
                SoTinChi = 3, MoTa = "Thiết kế và tối ưu cơ sở dữ liệu quan hệ"
            }
        );
        await db.SaveChangesAsync();
    }

    // Seed Classes, Enrollments, Sessions, Attendances nếu chưa có
    if (!db.Classes.Any())
    {
        var gvHung = db.Users.First(u => u.TenDangNhap == "gv_hung");
        var gvLan  = db.Users.First(u => u.TenDangNhap == "gv_lan");
        var cLTDD  = db.Courses.First(c => c.MaMonHoc == "LTDDDN");
        var cCSDL  = db.Courses.First(c => c.MaMonHoc == "CSDL");

        var lopLTDD = new VoAnhTuan_238060244_API.Models.VatClass
        {
            MaLopHoc = "23DTH_LTDD01",
            TenLopHoc = "Lập trình di động - Nhóm 01",
            CourseId = cLTDD.CourseId,
            GiangVienId = gvHung.UserId,
            NamHoc = 2026,
            HocKy = "HK1",
            ViDo = 10.7769, // Tọa độ mẫu trường ĐH (Hutech/HCM)
            KinhDo = 106.7009,
            BanKinhChoPhep = 100,
            DangHoatDong = true
        };

        var lopCSDL = new VoAnhTuan_238060244_API.Models.VatClass
        {
            MaLopHoc = "23DTH_CSDL01",
            TenLopHoc = "Cơ sở dữ liệu - Nhóm 01",
            CourseId = cCSDL.CourseId,
            GiangVienId = gvLan.UserId,
            NamHoc = 2026,
            HocKy = "HK1",
            ViDo = 10.7769,
            KinhDo = 106.7009,
            BanKinhChoPhep = 100,
            DangHoatDong = true
        };

        db.Classes.AddRange(lopLTDD, lopCSDL);
        await db.SaveChangesAsync();

        // Ghi danh 5 sinh viên vào lớp di động
        var sinhViens = db.Users.Where(u => u.TenDangNhap.StartsWith("sv_")).ToList();
        foreach (var sv in sinhViens)
        {
            db.Enrollments.Add(new VoAnhTuan_238060244_API.Models.VatEnrollment
            {
                ClassId = lopLTDD.ClassId,
                UserId = sv.UserId
            });
        }
        await db.SaveChangesAsync();

        // Tạo 5 buổi học cho lớp LTDD
        var danhSachBuoi = new List<VoAnhTuan_238060244_API.Models.VatSession>();
        for (int i = 1; i <= 5; i++)
        {
            danhSachBuoi.Add(new VoAnhTuan_238060244_API.Models.VatSession
            {
                ClassId = lopLTDD.ClassId,
                TieuDe = $"Buổi {i}: Kiến trúc và lập trình ứng dụng",
                ThoiGianBatDau = DateTime.UtcNow.AddDays(-7 * (5 - i)).AddHours(-2),
                ThoiGianKetThuc = DateTime.UtcNow.AddDays(-7 * (5 - i)),
                DangMoDiemDanh = (i == 5), // Buổi 5 đang mở điểm danh
                QrToken = (i == 5) ? VoAnhTuan_238060244_API.Services.QrLamMoiDichVu.SinhQrToken(1) : null,
                QrTokenCapNhatLuc = (i == 5) ? DateTime.UtcNow : null
            });
        }
        db.Sessions.AddRange(danhSachBuoi);
        await db.SaveChangesAsync();

        // Seed điểm danh cho 4 buổi đầu:
        // - Tuấn (238060244): 4 buổi có mặt (0% vắng)
        // - Mai: 3 buổi có mặt, 1 trễ (0% vắng)
        // - Bảo: 3 buổi có mặt, 1 vắng (25% vắng -> CẢNH BÁO / CẤM THI)
        // - Hoa: 2 buổi có mặt, 2 vắng (50% vắng -> CẤM THI)
        // - Khôi: 4 buổi có mặt (0% vắng)
        var uTuan = sinhViens.First(s => s.TenDangNhap == "sv_tuan");
        var uMai  = sinhViens.First(s => s.TenDangNhap == "sv_mai");
        var uBao  = sinhViens.First(s => s.TenDangNhap == "sv_bao");
        var uHoa  = sinhViens.First(s => s.TenDangNhap == "sv_hoa");
        var uKhoi = sinhViens.First(s => s.TenDangNhap == "sv_khoi");

        for (int i = 0; i < 4; i++)
        {
            var bId = danhSachBuoi[i].SessionId;

            // Tuấn luôn có mặt
            db.Attendances.Add(new VoAnhTuan_238060244_API.Models.VatAttendance
            {
                SessionId = bId, UserId = uTuan.UserId, TrangThai = "CóMặt",
                ThoiGianDiemDanh = danhSachBuoi[i].ThoiGianBatDau.AddMinutes(5),
                ViDoCheckIn = 10.7769, KinhDoCheckIn = 106.7009, KhoangCach = 5.2,
                ThietBiDaDung = "DEVICE_TUAN_PHONE_001"
            });

            // Mai: 3 có mặt, 1 trễ
            db.Attendances.Add(new VoAnhTuan_238060244_API.Models.VatAttendance
            {
                SessionId = bId, UserId = uMai.UserId, TrangThai = (i == 3) ? "TrễGiờ" : "CóMặt",
                ThoiGianDiemDanh = danhSachBuoi[i].ThoiGianBatDau.AddMinutes(i == 3 ? 20 : 8)
            });

            // Bảo: vắng buổi 2
            if (i != 1)
            {
                db.Attendances.Add(new VoAnhTuan_238060244_API.Models.VatAttendance
                {
                    SessionId = bId, UserId = uBao.UserId, TrangThai = "CóMặt",
                    ThoiGianDiemDanh = danhSachBuoi[i].ThoiGianBatDau.AddMinutes(6)
                });
            }
            else
            {
                db.Attendances.Add(new VoAnhTuan_238060244_API.Models.VatAttendance
                {
                    SessionId = bId, UserId = uBao.UserId, TrangThai = "VắngMặt"
                });
            }

            // Hoa: vắng buổi 1 và buổi 3 (vắng 2/4 = 50%)
            if (i == 1 || i == 3)
            {
                db.Attendances.Add(new VoAnhTuan_238060244_API.Models.VatAttendance
                {
                    SessionId = bId, UserId = uHoa.UserId, TrangThai = "CóMặt",
                    ThoiGianDiemDanh = danhSachBuoi[i].ThoiGianBatDau.AddMinutes(10)
                });
            }
            else
            {
                db.Attendances.Add(new VoAnhTuan_238060244_API.Models.VatAttendance
                {
                    SessionId = bId, UserId = uHoa.UserId, TrangThai = "VắngMặt"
                });
            }

            // Khôi luôn có mặt
            db.Attendances.Add(new VoAnhTuan_238060244_API.Models.VatAttendance
            {
                SessionId = bId, UserId = uKhoi.UserId, TrangThai = "CóMặt",
                ThoiGianDiemDanh = danhSachBuoi[i].ThoiGianBatDau.AddMinutes(7)
            });
        }

        // Seed thiết bị mẫu cho Tuấn
        db.Devices.Add(new VoAnhTuan_238060244_API.Models.VatDevice
        {
            UserId = uTuan.UserId,
            MaThietBi = "DEVICE_TUAN_PHONE_001",
            TenThietBi = "Samsung Galaxy S23 Ultra",
            HeDieuHanh = "Android 14",
            DuocPhepDiemDanh = true,
            NgayDangKy = DateTime.UtcNow,
            LanDangNhapCuoi = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}

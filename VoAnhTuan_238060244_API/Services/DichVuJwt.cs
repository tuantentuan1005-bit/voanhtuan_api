using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using VoAnhTuan_238060244_API.Models;

namespace VoAnhTuan_238060244_API.Services
{
    public interface IDichVuJwt
    {
        string TaoToken(VatUser nguoiDung, string vaiTro);
        ClaimsPrincipal? GiaiMaToken(string token);
    }

    public class DichVuJwt : IDichVuJwt
    {
        private readonly IConfiguration _cauHinh;

        public DichVuJwt(IConfiguration cauHinh)
        {
            _cauHinh = cauHinh;
        }

        public string TaoToken(VatUser nguoiDung, string vaiTro)
        {
            var jwt = _cauHinh.GetSection("CaiDatJwt");
            var khoaBiMat = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["BiMat"]!));
            var thongTinKy = new SigningCredentials(khoaBiMat, SecurityAlgorithms.HmacSha256);

            var danhSachClaim = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   nguoiDung.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, nguoiDung.Email),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier,     nguoiDung.UserId.ToString()),
                new Claim(ClaimTypes.Name,               nguoiDung.TenDangNhap),
                new Claim(ClaimTypes.Role,               vaiTro)
            };

            var thoiGianSong = int.Parse(jwt["ThoiGianSongPhut"] ?? "60");

            var token = new JwtSecurityToken(
                issuer:             jwt["NhaPhatHanh"],
                audience:           jwt["DoiTuong"],
                claims:             danhSachClaim,
                expires:            DateTime.UtcNow.AddMinutes(thoiGianSong),
                signingCredentials: thongTinKy
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? GiaiMaToken(string token)
        {
            try
            {
                var jwt = _cauHinh.GetSection("CaiDatJwt");
                var handler = new JwtSecurityTokenHandler();
                var thamSo = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["BiMat"]!)),
                    ValidateIssuer           = true,
                    ValidIssuer              = jwt["NhaPhatHanh"],
                    ValidateAudience         = true,
                    ValidAudience            = jwt["DoiTuong"],
                    ValidateLifetime         = true
                };
                return handler.ValidateToken(token, thamSo, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}

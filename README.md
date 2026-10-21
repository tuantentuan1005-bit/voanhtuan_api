# Web API Điểm Danh Sinh Viên - ASP.NET Core Web API (.NET 9)

**Học phần:** Xây dựng ứng dụng di động  
**Tác giả:** Võ Anh Tuấn  
**MSSV:** 238060244  
**Database:** `VoAnhTuan_238060244_DB` (SQL Server)  

---

## 🛠️ Công Nghệ Sử Dụng
* **Framework:** ASP.NET Core Web API (.NET 9)
* **ORM:** Entity Framework Core 9 (Code-First + Migrations)
* **Database:** Microsoft SQL Server
* **Authentication:** JWT Bearer Token + OTP Email (MailKit / SMTP)
* **QR Token:** QRCoder + Background Service xoay token mỗi 30s
* **GPS Haversine:** Thuật toán tính khoảng cách địa lý chính xác đến vị trí phòng học
* **Tài liệu API:** Swagger UI / OpenAPI

---

## 📂 Cấu Trúc Dự Án
```
VoAnhTuan_238060244_API/
├── VoAnhTuan_238060244_API/
│   ├── Controllers/        # XacThuc, NguoiDung, MonHoc, LopHoc, BuoiHoc, DiemDanh, BaoCao
│   ├── Data/               # AppDbContext, Migrations
│   ├── DTOs/               # AuthDTOs, CrudDTOs, PhanHoiChuan
│   ├── Helpers/            # TinhKhoangCachGps (Haversine)
│   ├── Models/             # 9 thực thể CSDL (VatRole, VatUser, VatCourse, ...)
│   ├── Services/           # DichVuJwt, DichVuEmail, QrLamMoiDichVu
│   ├── appsettings.json    # Cấu hình chuỗi kết nối, JWT, SMTP
│   └── Program.cs          # Pipeline cấu hình dịch vụ, Auto-migration, Seed data
├── VoAnhTuan_238060244_DB.sql  # Script T-SQL tạo CSDL và seed data độc lập cho SSMS
├── VoAnhTuan_238060244_API.sln
└── README.md
```

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy

### 1. Chuẩn Bị Cơ Sở Dữ Liệu
Chạy file script `VoAnhTuan_238060244_DB.sql` trong SQL Server Management Studio (SSMS), hoặc khi chạy API hệ thống sẽ tự động Migrate và Seed dữ liệu mẫu vào SQL Server (`localhost`).

### 2. Chạy Ứng Dụng
```bash
cd VoAnhTuan_238060244_API
dotnet run
```
Truy cập Swagger UI tại: **`http://localhost:5226`**

---

## 👤 Tài Khoản Mẫu

| Quyền | Tên đăng nhập | Mật khẩu | Vai trò |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin` | `Admin@123` | Quản trị toàn hệ thống |
| **Giảng viên** | `gv_hung` | `GiangVien@123` | Phụ trách môn Di động |
| **Giảng viên** | `gv_lan` | `GiangVien@123` | Phụ trách môn CSDL |
| **Sinh viên** | `sv_tuan` | `SinhVien@123` | Võ Anh Tuấn - 238060244 |
| **Sinh viên** | `sv_mai` | `SinhVien@123` | Lê Thị Mai - 238060001 |
| **Sinh viên** | `sv_bao` | `SinhVien@123` | Phạm Quốc Bảo - 238060002 (Cảnh báo vắng) |
| **Sinh viên** | `sv_hoa` | `SinhVien@123` | Đặng Thị Hoa - 238060004 (Cấm thi) |
| **Sinh viên** | `sv_khoi` | `SinhVien@123` | Nguyễn Minh Khôi - 238060003 |

*Lưu ý khi test đăng nhập*: Nhập Username/Password $\rightarrow$ mã OTP trả về trực tiếp trong response hoặc dùng mã test `123456` để verify.

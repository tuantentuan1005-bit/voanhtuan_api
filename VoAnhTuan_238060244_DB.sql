-- ============================================================================
-- ĐỒ ÁN / BÀI THI: XÂY DỰNG ỨNG DỤNG DI ĐỘNG - HỆ THỐNG ĐIỂM DANH SINH VIÊN
-- TÁC GIẢ: VÕ ANH TUẤN - MSSV: 238060244
-- DATABASE: VoAnhTuan_238060244_DB
-- ============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'VoAnhTuan_238060244_DB')
BEGIN
    CREATE DATABASE [VoAnhTuan_238060244_DB]
    COLLATE Vietnamese_CI_AS;
END
GO

USE [VoAnhTuan_238060244_DB];
GO

-- ============================================================================
-- 1. BẢNG ROLES (Vai trò người dùng)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId] INT IDENTITY(1,1) NOT NULL,
        [TenVaiTro] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC)
    );
END
GO

-- ============================================================================
-- 2. BẢNG USERS (Người dùng hệ thống: Admin, Giảng viên, Sinh viên)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId] INT IDENTITY(1,1) NOT NULL,
        [HoTen] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [TenDangNhap] NVARCHAR(100) NOT NULL,
        [MatKhauMaHoa] NVARCHAR(MAX) NOT NULL,
        [SoDienThoai] NVARCHAR(20) NULL,
        [MaSoSinhVien] NVARCHAR(50) NULL,
        [KichHoat] BIT NOT NULL CONSTRAINT [DF_Users_KichHoat] DEFAULT (1),
        [EmailDaXacThuc] BIT NOT NULL CONSTRAINT [DF_Users_EmailDaXacThuc] DEFAULT (0),
        [NgayTao] DATETIME2(7) NOT NULL CONSTRAINT [DF_Users_NgayTao] DEFAULT (SYSUTCDATETIME()),
        [RoleId] INT NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]([Email] ASC);
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_TenDangNhap] ON [dbo].[Users]([TenDangNhap] ASC);
    CREATE NONCLUSTERED INDEX [IX_Users_RoleId] ON [dbo].[Users]([RoleId] ASC);
END
GO

-- ============================================================================
-- 3. BẢNG COURSES (Môn học)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Courses]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Courses] (
        [CourseId] INT IDENTITY(1,1) NOT NULL,
        [MaMonHoc] NVARCHAR(20) NOT NULL,
        [TenMonHoc] NVARCHAR(200) NOT NULL,
        [SoTinChi] INT NOT NULL,
        [MoTa] NVARCHAR(500) NULL,
        [CongKhai] BIT NOT NULL CONSTRAINT [DF_Courses_CongKhai] DEFAULT (1),
        [NgayTao] DATETIME2(7) NOT NULL CONSTRAINT [DF_Courses_NgayTao] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Courses] PRIMARY KEY CLUSTERED ([CourseId] ASC)
    );
END
GO

-- ============================================================================
-- 4. BẢNG CLASSES (Lớp học - có tọa độ GPS để kiểm tra bán kính điểm danh)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Classes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Classes] (
        [ClassId] INT IDENTITY(1,1) NOT NULL,
        [MaLopHoc] NVARCHAR(30) NOT NULL,
        [TenLopHoc] NVARCHAR(200) NOT NULL,
        [NamHoc] INT NOT NULL,
        [HocKy] NVARCHAR(20) NULL,
        [ViDo] FLOAT NULL,
        [KinhDo] FLOAT NULL,
        [BanKinhChoPhep] FLOAT NOT NULL CONSTRAINT [DF_Classes_BanKinh] DEFAULT (100),
        [DangHoatDong] BIT NOT NULL CONSTRAINT [DF_Classes_DangHoatDong] DEFAULT (1),
        [NgayTao] DATETIME2(7) NOT NULL CONSTRAINT [DF_Classes_NgayTao] DEFAULT (SYSUTCDATETIME()),
        [CourseId] INT NOT NULL,
        [GiangVienId] INT NOT NULL,
        CONSTRAINT [PK_Classes] PRIMARY KEY CLUSTERED ([ClassId] ASC),
        CONSTRAINT [FK_Classes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses] ([CourseId]),
        CONSTRAINT [FK_Classes_Users_GiangVienId] FOREIGN KEY ([GiangVienId]) REFERENCES [dbo].[Users] ([UserId])
    );

    CREATE NONCLUSTERED INDEX [IX_Classes_CourseId] ON [dbo].[Classes]([CourseId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Classes_GiangVienId] ON [dbo].[Classes]([GiangVienId] ASC);
END
GO

-- ============================================================================
-- 5. BẢNG ENROLLMENTS (Sinh viên đăng ký vào lớp học)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Enrollments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Enrollments] (
        [EnrollmentId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ClassId] INT NOT NULL,
        [NgayDangKy] DATETIME2(7) NOT NULL CONSTRAINT [DF_Enrollments_NgayDangKy] DEFAULT (SYSUTCDATETIME()),
        [DaXoa] BIT NOT NULL CONSTRAINT [DF_Enrollments_DaXoa] DEFAULT (0),
        CONSTRAINT [PK_Enrollments] PRIMARY KEY CLUSTERED ([EnrollmentId] ASC),
        CONSTRAINT [FK_Enrollments_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [dbo].[Classes] ([ClassId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Enrollments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Enrollments_UserId_ClassId] ON [dbo].[Enrollments]([UserId] ASC, [ClassId] ASC);
END
GO

-- ============================================================================
-- 6. BẢNG SESSIONS (Buổi học - lưu QR Token động đổi mỗi 30s)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Sessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Sessions] (
        [SessionId] INT IDENTITY(1,1) NOT NULL,
        [TieuDe] NVARCHAR(100) NOT NULL,
        [ThoiGianBatDau] DATETIME2(7) NOT NULL,
        [ThoiGianKetThuc] DATETIME2(7) NOT NULL,
        [QrToken] NVARCHAR(MAX) NULL,
        [QrTokenCapNhatLuc] DATETIME2(7) NULL,
        [DangMoDiemDanh] BIT NOT NULL CONSTRAINT [DF_Sessions_DangMoDiemDanh] DEFAULT (0),
        [TongSoBuoi] INT NOT NULL CONSTRAINT [DF_Sessions_TongSoBuoi] DEFAULT (1),
        [NgayTao] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sessions_NgayTao] DEFAULT (SYSUTCDATETIME()),
        [ClassId] INT NOT NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY CLUSTERED ([SessionId] ASC),
        CONSTRAINT [FK_Sessions_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [dbo].[Classes] ([ClassId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_Sessions_ClassId] ON [dbo].[Sessions]([ClassId] ASC);
END
GO

-- ============================================================================
-- 7. BẢNG ATTENDANCES (Bản ghi điểm danh - GPS, Face Image, Device)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Attendances]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Attendances] (
        [AttendanceId] INT IDENTITY(1,1) NOT NULL,
        [SessionId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [TrangThai] NVARCHAR(30) NOT NULL CONSTRAINT [DF_Attendances_TrangThai] DEFAULT (N'VắngMặt'),
        [ThoiGianDiemDanh] DATETIME2(7) NULL,
        [ViDoCheckIn] FLOAT NULL,
        [KinhDoCheckIn] FLOAT NULL,
        [KhoangCach] FLOAT NULL,
        [DuongDanAnh] NVARCHAR(MAX) NULL,
        [ThietBiDaDung] NVARCHAR(200) NULL,
        [NgayTao] DATETIME2(7) NOT NULL CONSTRAINT [DF_Attendances_NgayTao] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Attendances] PRIMARY KEY CLUSTERED ([AttendanceId] ASC),
        CONSTRAINT [FK_Attendances_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[Sessions] ([SessionId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Attendances_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Attendances_SessionId_UserId] ON [dbo].[Attendances]([SessionId] ASC, [UserId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Attendances_UserId] ON [dbo].[Attendances]([UserId] ASC);
END
GO

-- ============================================================================
-- 8. BẢNG OTPCODES (Mã OTP cho Login và Email Verification)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[OtpCodes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OtpCodes] (
        [OtpId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [MaOtp] NVARCHAR(10) NOT NULL,
        [LoaiOtp] NVARCHAR(30) NOT NULL,
        [ThoiGianTao] DATETIME2(7) NOT NULL CONSTRAINT [DF_OtpCodes_Tao] DEFAULT (SYSUTCDATETIME()),
        [ThoiGianHetHan] DATETIME2(7) NOT NULL,
        [DaSuDung] BIT NOT NULL CONSTRAINT [DF_OtpCodes_DaDung] DEFAULT (0),
        CONSTRAINT [PK_OtpCodes] PRIMARY KEY CLUSTERED ([OtpId] ASC),
        CONSTRAINT [FK_OtpCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_OtpCodes_UserId] ON [dbo].[OtpCodes]([UserId] ASC);
END
GO

-- ============================================================================
-- 9. BẢNG DEVICES (Quản lý thiết bị điểm danh được cấp phép)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[Devices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Devices] (
        [DeviceId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [MaThietBi] NVARCHAR(300) NOT NULL,
        [TenThietBi] NVARCHAR(100) NULL,
        [HeDieuHanh] NVARCHAR(50) NULL,
        [DuocPhepDiemDanh] BIT NOT NULL CONSTRAINT [DF_Devices_DuocPhep] DEFAULT (1),
        [NgayDangKy] DATETIME2(7) NOT NULL CONSTRAINT [DF_Devices_NgayDangKy] DEFAULT (SYSUTCDATETIME()),
        [LanDangNhapCuoi] DATETIME2(7) NULL,
        CONSTRAINT [PK_Devices] PRIMARY KEY CLUSTERED ([DeviceId] ASC),
        CONSTRAINT [FK_Devices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_Devices_UserId] ON [dbo].[Devices]([UserId] ASC);
END
GO

-- ============================================================================
-- SEED DATA MẪU: 1 Admin, 2 Giảng viên, 5 Sinh viên, 2 Môn học, Lớp, Buổi, Điểm danh
-- ============================================================================

-- 1. Seed Roles
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles])
BEGIN
    SET IDENTITY_INSERT [dbo].[Roles] ON;
    INSERT INTO [dbo].[Roles] ([RoleId], [TenVaiTro]) VALUES
    (1, N'Admin'),
    (2, N'GiangVien'),
    (3, N'SinhVien');
    SET IDENTITY_INSERT [dbo].[Roles] OFF;
END
GO

-- 2. Seed Users (Mật khẩu: Admin@123, GiangVien@123, SinhVien@123 đã hash BCrypt)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users])
BEGIN
    SET IDENTITY_INSERT [dbo].[Users] ON;
    INSERT INTO [dbo].[Users] ([UserId], [HoTen], [Email], [TenDangNhap], [MatKhauMaHoa], [SoDienThoai], [MaSoSinhVien], [KichHoat], [EmailDaXacThuc], [RoleId], [NgayTao]) VALUES
    -- 1 Admin
    (1, N'Quản Trị Viên Hệ Thống', N'admin@diemdanh.edu.vn', N'admin', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0901234567', NULL, 1, 1, 1, SYSUTCDATETIME()),
    
    -- 2 Giảng viên
    (2, N'ThS. Nguyễn Văn Hùng', N'gv.hung@diemdanh.edu.vn', N'gv_hung', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0912345678', NULL, 1, 1, 2, SYSUTCDATETIME()),
    (3, N'TS. Trần Thị Lan', N'gv.lan@diemdanh.edu.vn', N'gv_lan', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0923456789', NULL, 1, 1, 2, SYSUTCDATETIME()),
    
    -- 5 Sinh viên (bao gồm Võ Anh Tuấn - 238060244)
    (4, N'Võ Anh Tuấn', N'sv.tuan@sinh.vien.edu.vn', N'sv_tuan', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0934567890', N'238060244', 1, 1, 3, SYSUTCDATETIME()),
    (5, N'Lê Thị Mai', N'sv.mai@sinh.vien.edu.vn', N'sv_mai', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0945678901', N'238060001', 1, 1, 3, SYSUTCDATETIME()),
    (6, N'Phạm Quốc Bảo', N'sv.bao@sinh.vien.edu.vn', N'sv_bao', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0956789012', N'238060002', 1, 1, 3, SYSUTCDATETIME()),
    (7, N'Nguyễn Minh Khôi', N'sv.khoi@sinh.vien.edu.vn', N'sv_khoi', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0967890123', N'238060003', 1, 1, 3, SYSUTCDATETIME()),
    (8, N'Đặng Thị Hoa', N'sv.hoa@sinh.vien.edu.vn', N'sv_hoa', N'$2a$11$0FfPqX3OaM8v4rFqR6u0g.f7kO9mP2hW3aL4qZ9oR7xU5cW1eB8mG', N'0978901234', N'238060004', 1, 1, 3, SYSUTCDATETIME());
    SET IDENTITY_INSERT [dbo].[Users] OFF;
END
GO

-- 3. Seed Courses (2 môn học)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Courses])
BEGIN
    SET IDENTITY_INSERT [dbo].[Courses] ON;
    INSERT INTO [dbo].[Courses] ([CourseId], [MaMonHoc], [TenMonHoc], [SoTinChi], [MoTa], [CongKhai], [NgayTao]) VALUES
    (1, N'LTDDDN', N'Xây dựng ứng dụng di động', 3, N'Lập trình Flutter, React Native, Web API .NET', 1, SYSUTCDATETIME()),
    (2, N'CSDL', N'Cơ sở dữ liệu nâng cao', 3, N'Tối ưu hóa truy vấn SQL Server, Indexing, EF Core', 1, SYSUTCDATETIME());
    SET IDENTITY_INSERT [dbo].[Courses] OFF;
END
GO

-- 4. Seed Classes
IF NOT EXISTS (SELECT 1 FROM [dbo].[Classes])
BEGIN
    SET IDENTITY_INSERT [dbo].[Classes] ON;
    INSERT INTO [dbo].[Classes] ([ClassId], [MaLopHoc], [TenLopHoc], [NamHoc], [HocKy], [ViDo], [KinhDo], [BanKinhChoPhep], [DangHoatDong], [CourseId], [GiangVienId], [NgayTao]) VALUES
    (1, N'23DTH_LTDD01', N'Lập trình di động - Nhóm 01', 2026, N'HK1', 10.7769, 106.7009, 100, 1, 1, 2, SYSUTCDATETIME()),
    (2, N'23DTH_CSDL01', N'Cơ sở dữ liệu nâng cao - Nhóm 01', 2026, N'HK1', 10.7769, 106.7009, 100, 1, 2, 3, SYSUTCDATETIME());
    SET IDENTITY_INSERT [dbo].[Classes] OFF;
END
GO

-- 5. Seed Enrollments (Ghi danh 5 sinh viên vào lớp di động)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Enrollments])
BEGIN
    INSERT INTO [dbo].[Enrollments] ([UserId], [ClassId], [NgayDangKy], [DaXoa]) VALUES
    (4, 1, SYSUTCDATETIME(), 0),
    (5, 1, SYSUTCDATETIME(), 0),
    (6, 1, SYSUTCDATETIME(), 0),
    (7, 1, SYSUTCDATETIME(), 0),
    (8, 1, SYSUTCDATETIME(), 0);
END
GO

-- 6. Seed Sessions (5 buổi học, buổi 5 đang mở điểm danh)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Sessions])
BEGIN
    SET IDENTITY_INSERT [dbo].[Sessions] ON;
    INSERT INTO [dbo].[Sessions] ([SessionId], [ClassId], [TieuDe], [ThoiGianBatDau], [ThoiGianKetThuc], [DangMoDiemDanh], [QrToken], [QrTokenCapNhatLuc], [TongSoBuoi], [NgayTao]) VALUES
    (1, 1, N'Buổi 1: Giới thiệu kiến trúc Web API', DATEADD(DAY, -28, SYSUTCDATETIME()), DATEADD(DAY, -28, SYSUTCDATETIME()), 0, NULL, NULL, 1, SYSUTCDATETIME()),
    (2, 1, N'Buổi 2: Thiết kế Database & EF Core', DATEADD(DAY, -21, SYSUTCDATETIME()), DATEADD(DAY, -21, SYSUTCDATETIME()), 0, NULL, NULL, 1, SYSUTCDATETIME()),
    (3, 1, N'Buổi 3: Xác thực JWT & OTP Email', DATEADD(DAY, -14, SYSUTCDATETIME()), DATEADD(DAY, -14, SYSUTCDATETIME()), 0, NULL, NULL, 1, SYSUTCDATETIME()),
    (4, 1, N'Buổi 4: Điểm danh QR & Định vị GPS', DATEADD(DAY, -7, SYSUTCDATETIME()), DATEADD(DAY, -7, SYSUTCDATETIME()), 0, NULL, NULL, 1, SYSUTCDATETIME()),
    (5, 1, N'Buổi 5: Báo cáo & Kiểm thử toàn diện', SYSUTCDATETIME(), DATEADD(HOUR, 2, SYSUTCDATETIME()), 1, N'SESSION_5_ACTIVE_QR_TOKEN', SYSUTCDATETIME(), 1, SYSUTCDATETIME());
    SET IDENTITY_INSERT [dbo].[Sessions] OFF;
END
GO

-- 7. Seed Attendances (Dữ liệu mẫu cho báo cáo cấm thi: Hoa vắng 50%, Bảo vắng 25%)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Attendances])
BEGIN
    INSERT INTO [dbo].[Attendances] ([SessionId], [UserId], [TrangThai], [ThoiGianDiemDanh], [ViDoCheckIn], [KinhDoCheckIn], [KhoangCach], [ThietBiDaDung], [NgayTao]) VALUES
    -- Buổi 1
    (1, 4, N'CóMặt', DATEADD(MINUTE, 5, DATEADD(DAY, -28, SYSUTCDATETIME())), 10.7769, 106.7009, 2.5, N'DEVICE_TUAN_PHONE_001', SYSUTCDATETIME()),
    (1, 5, N'CóMặt', DATEADD(MINUTE, 7, DATEADD(DAY, -28, SYSUTCDATETIME())), 10.7769, 106.7009, 15.2, N'DEVICE_MAI', SYSUTCDATETIME()),
    (1, 6, N'CóMặt', DATEADD(MINUTE, 8, DATEADD(DAY, -28, SYSUTCDATETIME())), 10.7769, 106.7009, 8.4, N'DEVICE_BAO', SYSUTCDATETIME()),
    (1, 7, N'CóMặt', DATEADD(MINUTE, 6, DATEADD(DAY, -28, SYSUTCDATETIME())), 10.7769, 106.7009, 12.0, N'DEVICE_KHOI', SYSUTCDATETIME()),
    (1, 8, N'VắngMặt', NULL, NULL, NULL, NULL, NULL, SYSUTCDATETIME()),

    -- Buổi 2
    (2, 4, N'CóMặt', DATEADD(MINUTE, 5, DATEADD(DAY, -21, SYSUTCDATETIME())), 10.7769, 106.7009, 3.1, N'DEVICE_TUAN_PHONE_001', SYSUTCDATETIME()),
    (2, 5, N'CóMặt', DATEADD(MINUTE, 9, DATEADD(DAY, -21, SYSUTCDATETIME())), 10.7769, 106.7009, 14.0, N'DEVICE_MAI', SYSUTCDATETIME()),
    (2, 6, N'VắngMặt', NULL, NULL, NULL, NULL, NULL, SYSUTCDATETIME()),
    (2, 7, N'CóMặt', DATEADD(MINUTE, 5, DATEADD(DAY, -21, SYSUTCDATETIME())), 10.7769, 106.7009, 9.8, N'DEVICE_KHOI', SYSUTCDATETIME()),
    (2, 8, N'CóMặt', DATEADD(MINUTE, 10, DATEADD(DAY, -21, SYSUTCDATETIME())), 10.7769, 106.7009, 18.5, N'DEVICE_HOA', SYSUTCDATETIME()),

    -- Buổi 3
    (3, 4, N'CóMặt', DATEADD(MINUTE, 4, DATEADD(DAY, -14, SYSUTCDATETIME())), 10.7769, 106.7009, 1.8, N'DEVICE_TUAN_PHONE_001', SYSUTCDATETIME()),
    (3, 5, N'TrễGiờ', DATEADD(MINUTE, 25, DATEADD(DAY, -14, SYSUTCDATETIME())), 10.7769, 106.7009, 22.0, N'DEVICE_MAI', SYSUTCDATETIME()),
    (3, 6, N'CóMặt', DATEADD(MINUTE, 5, DATEADD(DAY, -14, SYSUTCDATETIME())), 10.7769, 106.7009, 7.3, N'DEVICE_BAO', SYSUTCDATETIME()),
    (3, 7, N'CóMặt', DATEADD(MINUTE, 6, DATEADD(DAY, -14, SYSUTCDATETIME())), 10.7769, 106.7009, 11.2, N'DEVICE_KHOI', SYSUTCDATETIME()),
    (3, 8, N'VắngMặt', NULL, NULL, NULL, NULL, NULL, SYSUTCDATETIME()),

    -- Buổi 4
    (4, 4, N'CóMặt', DATEADD(MINUTE, 3, DATEADD(DAY, -7, SYSUTCDATETIME())), 10.7769, 106.7009, 4.2, N'DEVICE_TUAN_PHONE_001', SYSUTCDATETIME()),
    (4, 5, N'CóMặt', DATEADD(MINUTE, 6, DATEADD(DAY, -7, SYSUTCDATETIME())), 10.7769, 106.7009, 10.5, N'DEVICE_MAI', SYSUTCDATETIME()),
    (4, 6, N'CóMặt', DATEADD(MINUTE, 8, DATEADD(DAY, -7, SYSUTCDATETIME())), 10.7769, 106.7009, 6.7, N'DEVICE_BAO', SYSUTCDATETIME()),
    (4, 7, N'CóMặt', DATEADD(MINUTE, 4, DATEADD(DAY, -7, SYSUTCDATETIME())), 10.7769, 106.7009, 8.9, N'DEVICE_KHOI', SYSUTCDATETIME()),
    (4, 8, N'CóMặt', DATEADD(MINUTE, 11, DATEADD(DAY, -7, SYSUTCDATETIME())), 10.7769, 106.7009, 16.3, N'DEVICE_HOA', SYSUTCDATETIME());
END
GO

-- 8. Seed Devices
IF NOT EXISTS (SELECT 1 FROM [dbo].[Devices])
BEGIN
    INSERT INTO [dbo].[Devices] ([UserId], [MaThietBi], [TenThietBi], [HeDieuHanh], [DuocPhepDiemDanh], [NgayDangKy], [LanDangNhapCuoi]) VALUES
    (4, N'DEVICE_TUAN_PHONE_001', N'Samsung Galaxy S23 Ultra', N'Android 14', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (5, N'DEVICE_MAI', N'iPhone 15 Pro Max', N'iOS 17.5', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (6, N'DEVICE_BAO', N'Xiaomi 13T Pro', N'HyperOS', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (7, N'DEVICE_KHOI', N'Google Pixel 8', N'Android 14', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (8, N'DEVICE_HOA', N'Oppo Reno 10', N'ColorOS 14', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- ============================================================================
-- TRUY VẤN KIỂM TRA MẪU (TEST REPORT CẤM THI BẰNG SQL)
-- ============================================================================
SELECT 
    c.MaLopHoc,
    c.TenLopHoc,
    u.MaSoSinhVien,
    u.HoTen AS HoTenSinhVien,
    COUNT(s.SessionId) AS TongSoBuoiLop,
    SUM(CASE WHEN a.TrangThai = N'CóMặt' THEN 1 ELSE 0 END) AS SoBuoiCoMat,
    SUM(CASE WHEN a.TrangThai = N'TrễGiờ' THEN 1 ELSE 0 END) AS SoBuoiTre,
    SUM(CASE WHEN a.TrangThai = N'VắngMặt' OR a.TrangThai IS NULL THEN 1 ELSE 0 END) AS SoBuoiVang,
    CAST(SUM(CASE WHEN a.TrangThai = N'VắngMặt' OR a.TrangThai IS NULL THEN 1 ELSE 0 END) * 100.0 / COUNT(s.SessionId) AS DECIMAL(5,2)) AS TyLeVangPhanTram,
    CASE 
        WHEN (SUM(CASE WHEN a.TrangThai = N'VắngMặt' OR a.TrangThai IS NULL THEN 1 ELSE 0 END) * 1.0 / COUNT(s.SessionId)) >= 0.20 THEN N'CẤM THI'
        WHEN (SUM(CASE WHEN a.TrangThai = N'VắngMặt' OR a.TrangThai IS NULL THEN 1 ELSE 0 END) * 1.0 / COUNT(s.SessionId)) >= 0.15 THEN N'CẢNH BÁO'
        ELSE N'BÌNH THƯỜNG'
    END AS MucDoCanhBao
FROM [dbo].[Classes] c
JOIN [dbo].[Enrollments] e ON c.ClassId = e.ClassId AND e.DaXoa = 0
JOIN [dbo].[Users] u ON e.UserId = u.UserId
JOIN [dbo].[Sessions] s ON c.ClassId = s.ClassId
LEFT JOIN [dbo].[Attendances] a ON s.SessionId = a.SessionId AND u.UserId = a.UserId
WHERE c.ClassId = 1
GROUP BY c.MaLopHoc, c.TenLopHoc, u.MaSoSinhVien, u.HoTen
ORDER BY TyLeVangPhanTram DESC;
GO

PRINT N'>>> VoAnhTuan_238060244_DB: Khởi tạo CSDL và Seed dữ liệu hoàn tất! <<<';
GO

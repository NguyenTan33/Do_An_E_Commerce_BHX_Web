-- ===================================================================================
-- BẢN KỊCH BẢN TẠO VÀ CẬP NHẬT CƠ SỞ DỮ LIỆU SQL SERVER HỆ THỐNG BÁCH HÓA XANH (BHX)
-- Tự động kiểm tra và thêm các Bảng (Tables) & Cột (Columns) mới đồng bộ 100% với Code C#
-- ===================================================================================

USE [Do_An_E_Commerce_BHX]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

PRINT N'----------------------------------------------------------------------'
PRINT N'1. KHỞI TẠO CÁC BẢNG NGHỆ NGHIỆP: VÍ TIỀN, YÊU CẦU RÚT TIỀN, NHẬT KÝ VÍ'
PRINT N'----------------------------------------------------------------------'

-- 1.1 Bảng UserWallet (Ví tiền cá nhân của khách hàng)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'UserWallet')
BEGIN
    CREATE TABLE [dbo].[UserWallet] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(128) NOT NULL UNIQUE,
        [Balance] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_UserWallet_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    PRINT N'✅ Đã tạo thành công bảng [UserWallet]';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng [UserWallet] đã tồn tại.';
END
GO

-- 1.2 Bảng WithdrawalRequest (Yêu cầu rút tiền về ngân hàng)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'WithdrawalRequest')
BEGIN
    CREATE TABLE [dbo].[WithdrawalRequest] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(128) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [BankName] NVARCHAR(100) NOT NULL,
        [AccountNumber] NVARCHAR(50) NOT NULL,
        [AccountHolderName] NVARCHAR(100) NOT NULL,
        [Status] INT NOT NULL DEFAULT 0, -- 0: Đang rút (Pending), 1: Thành công (Completed), 2: Thất bại (Rejected)
        [ExpectedPayoutDate] DATETIME NOT NULL,
        [AdminNote] NVARCHAR(500) NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ProcessedDate] DATETIME NULL,
        CONSTRAINT [FK_WithdrawalRequest_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    PRINT N'✅ Đã tạo thành công bảng [WithdrawalRequest]';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng [WithdrawalRequest] đã tồn tại.';
END
GO

-- 1.3 Bảng WalletTransaction (Nhật ký lịch sử biến động số dư ví)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'WalletTransaction')
BEGIN
    CREATE TABLE [dbo].[WalletTransaction] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(128) NOT NULL,
        [TransactionType] INT NOT NULL, -- 0: Hoàn tiền đơn hàng (+), 1: Yêu cầu rút tiền (-), 2: Hoàn lại tiền rút thất bại (+)
        [Amount] DECIMAL(18,2) NOT NULL,
        [BalanceBefore] DECIMAL(18,2) NOT NULL,
        [BalanceAfter] DECIMAL(18,2) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [OrderId] INT NULL,
        [WithdrawalRequestId] INT NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_WalletTransaction_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    PRINT N'✅ Đã tạo thành công bảng [WalletTransaction]';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng [WalletTransaction] đã tồn tại.';
END
GO

PRINT N'----------------------------------------------------------------------'
PRINT N'2. KHỞI TẠO BẢNG SỔ ĐỊA CHỈ & NHẬT KÝ HÀNH VI NGƯỜI DÙNG'
PRINT N'----------------------------------------------------------------------'

-- 2.1 Bảng UserAddress (Sổ địa chỉ nhận hàng của khách)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'UserAddress')
BEGIN
    CREATE TABLE [dbo].[UserAddress] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(128) NOT NULL,
        [ReceiverName] NVARCHAR(100) NOT NULL,
        [ReceiverPhone] NVARCHAR(15) NOT NULL,
        [AddressDetail] NVARCHAR(255) NOT NULL,
        [IsDefault] BIT NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_UserAddress_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    PRINT N'✅ Đã tạo thành công bảng [UserAddress]';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng [UserAddress] đã tồn tại.';
END
GO

-- 2.2 Bảng UserBehaviorLog (Phân tích hành vi người dùng & theo dõi)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'UserBehaviorLog')
BEGIN
    CREATE TABLE [dbo].[UserBehaviorLog] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(128) NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [TargetType] NVARCHAR(50) NULL,
        [TargetId] NVARCHAR(100) NULL,
        [TargetName] NVARCHAR(255) NULL,
        [ExtraDataJson] NVARCHAR(MAX) NULL,
        [DeviceType] NVARCHAR(50) NULL,
        [IPAddress] NVARCHAR(50) NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT N'✅ Đã tạo thành công bảng [UserBehaviorLog]';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng [UserBehaviorLog] đã tồn tại.';
END
GO

PRINT N'----------------------------------------------------------------------'
PRINT N'3. CẬP NHẬT CÁC CỘT Bổ SUNG CHO BẢNG SẢN PHẨM, ĐƠN HÀNG, KHÁCH HÀNG'
PRINT N'----------------------------------------------------------------------'

-- 3.1 Bổ sung cột cho bảng [Products] / [Product]
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Products')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = N'OriginalPrice')
        ALTER TABLE [dbo].[Products] ADD [OriginalPrice] DECIMAL(18,2) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = N'DiscountPercent')
        ALTER TABLE [dbo].[Products] ADD [DiscountPercent] INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = N'Unit')
        ALTER TABLE [dbo].[Products] ADD [Unit] NVARCHAR(50) NULL;
    PRINT N'✅ Đã cập nhật cột cho [Products]';
END
GO

-- 3.2 Bổ sung cột cho bảng [Orders] / [Order] (Tích điểm & Giảm giá điểm)
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Orders')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'EarnedPoints')
        ALTER TABLE [dbo].[Orders] ADD [EarnedPoints] INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'UsedPoints')
        ALTER TABLE [dbo].[Orders] ADD [UsedPoints] INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'PointDiscountAmount')
        ALTER TABLE [dbo].[Orders] ADD [PointDiscountAmount] FLOAT NOT NULL DEFAULT 0;
    PRINT N'✅ Đã cập nhật cột cho [Orders]';
END
GO

-- 3.3 Bổ sung cột cho bảng [AspNetUsers] (Điểm thưởng & Hạng thành viên)
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'AspNetUsers')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'RewardPoints')
        ALTER TABLE [dbo].[AspNetUsers] ADD [RewardPoints] INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'CustomerTier')
        ALTER TABLE [dbo].[AspNetUsers] ADD [CustomerTier] INT NOT NULL DEFAULT 0;
    PRINT N'✅ Đã cập nhật cột cho [AspNetUsers]';
END
GO

PRINT N'======================================================================'
PRINT N'🎉 HOÀN TẤT ĐỒNG BỘ CƠ SỞ DỮ LIỆU SQL SERVER CHO HỆ THỐNG BHX!'
PRINT N'======================================================================'

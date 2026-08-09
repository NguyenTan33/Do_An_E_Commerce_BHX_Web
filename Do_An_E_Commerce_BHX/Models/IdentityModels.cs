using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public int LoyaltyPoints { get; set; } = 0; // Điểm tích lũy Bách Hóa Xanh (100 điểm = 1.000đ)
        public System.DateTime? LastActivityDate { get; set; } // Thời điểm hoạt động / tương tác gần nhất

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public DbSet<Entities.UserAddress> UserAddresses { get; set; }
        public DbSet<Entities.Cart> Cart { get; set; }
        public DbSet<Entities.CartDetail> CartDetail { get; set; }
        public DbSet<Entities.Category> Category { get; set; }
        public DbSet<Entities.Order> Order { get; set; }
        public DbSet<Entities.OrderDetail> OrderDetail { get; set; }
        public DbSet<Entities.Preview> Preview { get; set; }
        public DbSet<Entities.Product> Product { get; set; }
        public DbSet<Entities.ProductUnit> ProductUnit { get; set; }
        public DbSet<Entities.Promotion> Promotion { get; set; }
        public DbSet<Entities.UserPromotion> UserPromotion { get; set; }
        public DbSet<Entities.Question> Question { get; set; }
        public DbSet<Entities.Review> Review { get; set; }
        public DbSet<Entities.Waranty> Waranty { get; set; }
        public DbSet<Entities.UserBehaviorLog> UserBehaviorLog { get; set; }
        public DbSet<Entities.UserWallet> UserWallet { get; set; }
        public DbSet<Entities.WithdrawalRequest> WithdrawalRequest { get; set; }
        public DbSet<Entities.WalletTransaction> WalletTransaction { get; set; }

        private static bool _isColumnsChecked = false;
        private static readonly object _lockObj = new object();

        public static void EnsureProductColumnsExist(ApplicationDbContext db)
        {
            if (_isColumnsChecked) return;
            lock (_lockObj)
            {
                if (_isColumnsChecked) return;
                try
                {
                    string sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'UserBehaviorLog')
                    BEGIN
                        CREATE TABLE [dbo].[UserBehaviorLog] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [SessionId] NVARCHAR(100) NULL,
                            [UserId] NVARCHAR(128) NULL,
                            [EventType] NVARCHAR(50) NOT NULL,
                            [TargetId] INT NULL,
                            [TargetName] NVARCHAR(255) NULL,
                            [DurationSeconds] INT NULL,
                            [ScrollPercent] INT NULL,
                            [ReferrerUrl] NVARCHAR(500) NULL,
                            [PageLoadMs] INT NULL,
                            [ExtraDataJson] NVARCHAR(MAX) NULL,
                            [DeviceType] NVARCHAR(50) NULL,
                            [IPAddress] NVARCHAR(50) NULL,
                            [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'UserWallet')
                    BEGIN
                        CREATE TABLE [dbo].[UserWallet] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] NVARCHAR(128) NOT NULL UNIQUE,
                            [Balance] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'WithdrawalRequest')
                    BEGIN
                        CREATE TABLE [dbo].[WithdrawalRequest] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] NVARCHAR(128) NOT NULL,
                            [Amount] DECIMAL(18,2) NOT NULL,
                            [BankName] NVARCHAR(100) NOT NULL,
                            [AccountNumber] NVARCHAR(50) NOT NULL,
                            [AccountHolderName] NVARCHAR(100) NOT NULL,
                            [Status] INT NOT NULL DEFAULT 0,
                            [ExpectedPayoutDate] DATETIME NOT NULL,
                            [AdminNote] NVARCHAR(500) NULL,
                            [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
                            [ProcessedDate] DATETIME NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'WalletTransaction')
                    BEGIN
                        CREATE TABLE [dbo].[WalletTransaction] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] NVARCHAR(128) NOT NULL,
                            [TransactionType] INT NOT NULL,
                            [Amount] DECIMAL(18,2) NOT NULL,
                            [BalanceBefore] DECIMAL(18,2) NOT NULL,
                            [BalanceAfter] DECIMAL(18,2) NOT NULL,
                            [Description] NVARCHAR(500) NOT NULL,
                            [OrderId] INT NULL,
                            [WithdrawalRequestId] INT NULL,
                            [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Products')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'Unit')
                            ALTER TABLE [dbo].[Products] ADD [Unit] NVARCHAR(50) DEFAULT N'Cái' NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'UnitMultiplier')
                            ALTER TABLE [dbo].[Products] ADD [UnitMultiplier] INT DEFAULT 1 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'ParentProductId')
                            ALTER TABLE [dbo].[Products] ADD [ParentProductId] INT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'PackagingTag')
                            ALTER TABLE [dbo].[Products] ADD [PackagingTag] NVARCHAR(100) NULL;
                    END

                    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Product')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'Unit')
                            ALTER TABLE [dbo].[Product] ADD [Unit] NVARCHAR(50) DEFAULT N'Cái' NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'UnitMultiplier')
                            ALTER TABLE [dbo].[Product] ADD [UnitMultiplier] INT DEFAULT 1 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'ParentProductId')
                            ALTER TABLE [dbo].[Product] ADD [ParentProductId] INT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'PackagingTag')
                            ALTER TABLE [dbo].[Product] ADD [PackagingTag] NVARCHAR(100) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'ProductUnit')
                    BEGIN
                        CREATE TABLE [dbo].[ProductUnit] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [ProductId] INT NOT NULL,
                            [UnitName] NVARCHAR(100) NOT NULL,
                            [Price] DECIMAL(18,2) NOT NULL,
                            [ConversionFactor] INT DEFAULT 1 NOT NULL,
                            [IsDefault] BIT DEFAULT 0 NOT NULL
                        );
                    END

                    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'AspNetUsers')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'FullName')
                            ALTER TABLE [dbo].[AspNetUsers] ADD [FullName] NVARCHAR(255) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'Address')
                            ALTER TABLE [dbo].[AspNetUsers] ADD [Address] NVARCHAR(500) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'LoyaltyPoints')
                            ALTER TABLE [dbo].[AspNetUsers] ADD [LoyaltyPoints] INT DEFAULT 0 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'LastActivityDate')
                            ALTER TABLE [dbo].[AspNetUsers] ADD [LastActivityDate] DATETIME NULL;
                    END
                ";
                    db.Database.ExecuteSqlCommand(sql);
                    _isColumnsChecked = true;
                }
                catch { }
            }
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}
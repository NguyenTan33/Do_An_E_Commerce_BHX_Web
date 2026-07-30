using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class VoucherEvaluationResult
    {
        public bool IsEligible { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public double MinOrderAmount { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public double CalculatedDiscount { get; set; }
        public string ReasonIfNotEligible { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsSaved { get; set; }
    }

    public class VoucherService
    {
        private readonly ApplicationDbContext _db;

        public VoucherService(ApplicationDbContext db)
        {
            _db = db;
        }

        public void EnsureDatabaseColumnsExist()
        {
            try
            {
                string sql = @"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Promotions')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'MinOrderAmount')
                            ALTER TABLE [dbo].[Promotions] ADD [MinOrderAmount] FLOAT DEFAULT 0 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'CategoryId')
                            ALTER TABLE [dbo].[Promotions] ADD [CategoryId] INT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'MaxDiscountAmount')
                            ALTER TABLE [dbo].[Promotions] ADD [MaxDiscountAmount] FLOAT DEFAULT 0 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'Description')
                            ALTER TABLE [dbo].[Promotions] ADD [Description] NVARCHAR(255) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'UsageLimit')
                            ALTER TABLE [dbo].[Promotions] ADD [UsageLimit] INT DEFAULT 100 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'UsedCount')
                            ALTER TABLE [dbo].[Promotions] ADD [UsedCount] INT DEFAULT 0 NOT NULL;
                    END

                    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Promotion')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotion]') AND name = 'MinOrderAmount')
                            ALTER TABLE [dbo].[Promotion] ADD [MinOrderAmount] FLOAT DEFAULT 0 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotion]') AND name = 'CategoryId')
                            ALTER TABLE [dbo].[Promotion] ADD [CategoryId] INT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotion]') AND name = 'MaxDiscountAmount')
                            ALTER TABLE [dbo].[Promotion] ADD [MaxDiscountAmount] FLOAT DEFAULT 0 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotion]') AND name = 'Description')
                            ALTER TABLE [dbo].[Promotion] ADD [Description] NVARCHAR(255) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotion]') AND name = 'UsageLimit')
                            ALTER TABLE [dbo].[Promotion] ADD [UsageLimit] INT DEFAULT 100 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotion]') AND name = 'UsedCount')
                            ALTER TABLE [dbo].[Promotion] ADD [UsedCount] INT DEFAULT 0 NOT NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'UserPromotion')
                    BEGIN
                        CREATE TABLE [dbo].[UserPromotion] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] NVARCHAR(128) NOT NULL,
                            [PromotionId] INT NOT NULL,
                            [IsUsed] BIT DEFAULT 0 NOT NULL,
                            [SavedDate] DATETIME DEFAULT GETDATE() NOT NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'UserPromotions')
                    BEGIN
                        CREATE TABLE [dbo].[UserPromotions] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] NVARCHAR(128) NOT NULL,
                            [PromotionId] INT NOT NULL,
                            [IsUsed] BIT DEFAULT 0 NOT NULL,
                            [SavedDate] DATETIME DEFAULT GETDATE() NOT NULL
                        );
                    END
                ";

                _db.Database.ExecuteSqlCommand(sql);
            }
            catch { }
        }

        // Tự động khởi tạo các mã giảm giá mẫu phong phú (Presets)
        public void SeedSampleVouchersIfEmpty()
        {
            EnsureDatabaseColumnsExist();

            try
            {
                // Kiểm tra danh mục Dầu ăn để gán sample
                var dauAnCat = _db.Category.FirstOrDefault(c => c.Name.Contains("Dầu") || c.Name.Contains("Gia vị"));

                // 1. Mã đơn 0đ
                if (!_db.Promotion.Any(p => p.Code == "BHXFREE0K"))
                {
                    _db.Promotion.Add(new Promotion
                    {
                        Code = "BHXFREE0K",
                        DiscountValue = 10000,
                        percentDiscount = 0,
                        MinOrderAmount = 0,
                        CategoryId = null,
                        Description = "Đơn từ 0đ - Giảm ngay 10.000 VNĐ cho Tất cả sản phẩm",
                        EffectiveDate = DateTime.Now.AddDays(-1),
                        ExpiryDate = DateTime.Now.AddDays(60),
                        IsActive = true
                    });
                }

                // 2. Mã dành riêng cho Dầu ăn (Đơn từ 50k)
                if (!_db.Promotion.Any(p => p.Code == "DAUAN50K"))
                {
                    _db.Promotion.Add(new Promotion
                    {
                        Code = "DAUAN50K",
                        DiscountValue = 0,
                        percentDiscount = 15,
                        MaxDiscountAmount = 30000,
                        MinOrderAmount = 50000,
                        CategoryId = dauAnCat != null ? (int?)dauAnCat.Id : null,
                        Description = "Đơn từ 50k - Giảm 15% (tối đa 30k) dành riêng sản phẩm Dầu ăn",
                        EffectiveDate = DateTime.Now.AddDays(-1),
                        ExpiryDate = DateTime.Now.AddDays(60),
                        IsActive = true
                    });
                }

                // 3. Mã Đơn từ 50k - Giảm 20k
                if (!_db.Promotion.Any(p => p.Code == "BHX50K"))
                {
                    _db.Promotion.Add(new Promotion
                    {
                        Code = "BHX50K",
                        DiscountValue = 20000,
                        percentDiscount = 0,
                        MinOrderAmount = 50000,
                        CategoryId = null,
                        Description = "Đơn từ 50k - Giảm ngay 20.000 VNĐ cho Tất cả sản phẩm",
                        EffectiveDate = DateTime.Now.AddDays(-1),
                        ExpiryDate = DateTime.Now.AddDays(60),
                        IsActive = true
                    });
                }

                // 4. Mã Đơn VIP từ 200k - Giảm 50k
                if (!_db.Promotion.Any(p => p.Code == "BHXVIP200K"))
                {
                    _db.Promotion.Add(new Promotion
                    {
                        Code = "BHXVIP200K",
                        DiscountValue = 50000,
                        percentDiscount = 0,
                        MinOrderAmount = 200000,
                        CategoryId = null,
                        Description = "Đơn VIP từ 200k - Giảm ngay 50.000 VNĐ cho Tất cả sản phẩm",
                        EffectiveDate = DateTime.Now.AddDays(-1),
                        ExpiryDate = DateTime.Now.AddDays(60),
                        IsActive = true
                    });
                }

                _db.SaveChanges();
            }
            catch { }
        }

        // Đánh giá mã giảm giá đối với danh sách món ăn trong Giỏ hàng
        public VoucherEvaluationResult EvaluateVoucher(Promotion promo, List<CartDetail> items, string userId = null)
        {
            var result = new VoucherEvaluationResult
            {
                Code = promo.Code,
                Description = !string.IsNullOrWhiteSpace(promo.Description) ? promo.Description : $"Mã giảm giá {promo.Code}",
                MinOrderAmount = promo.MinOrderAmount,
                CategoryId = promo.CategoryId,
                CategoryName = promo.Category != null ? promo.Category.Name : "Tất cả sản phẩm",
                ExpiryDate = promo.ExpiryDate,
                IsEligible = false,
                CalculatedDiscount = 0,
                ReasonIfNotEligible = ""
            };

            var now = DateTime.Now;
            if (!promo.IsActive || promo.EffectiveDate > now || promo.ExpiryDate < now || (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit))
            {
                if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
                {
                    if (promo.IsActive)
                    {
                        promo.IsActive = false; // Tự động tắt mã khi hết số lượng
                        try { _db.SaveChanges(); } catch { }
                    }
                    result.ReasonIfNotEligible = $"Mã giảm giá [{promo.Code}] đã HẾT SỐ LƯỢNG phát hành ({promo.UsedCount}/{promo.UsageLimit}) và bị tắt ngắt!";
                }
                else
                {
                    result.ReasonIfNotEligible = "Mã giảm giá đã hết hạn sử dụng hoặc chưa được kích hoạt.";
                }
                return result;
            }

            if (items == null || !items.Any())
            {
                result.ReasonIfNotEligible = "Giỏ hàng của bạn đang trống!";
                return result;
            }

            // Lọc danh sách món thuộc danh mục áp dụng (nếu CategoryId được quy định)
            var eligibleItems = items;
            if (promo.CategoryId.HasValue && promo.CategoryId.Value > 0)
            {
                eligibleItems = items.Where(i => i.Product != null && i.Product.CategoryId == promo.CategoryId.Value).ToList();
                if (!eligibleItems.Any())
                {
                    string catName = promo.Category != null ? promo.Category.Name : "Danh mục yêu cầu";
                    result.ReasonIfNotEligible = $"Giỏ hàng của bạn chưa có sản phẩm thuộc danh mục [{catName}].";
                    return result;
                }
            }

            // Tính tổng tiền các sản phẩm hợp lệ
            double applicableSubtotal = eligibleItems.Sum(i => (i.Product != null ? (double)i.Product.Price : 0) * i.Quantity);
            double totalCartSubtotal = items.Sum(i => (i.Product != null ? (double)i.Product.Price : 0) * i.Quantity);

            // Kiểm tra điều kiện đơn hàng tối thiểu
            if (totalCartSubtotal < promo.MinOrderAmount)
            {
                double diff = promo.MinOrderAmount - totalCartSubtotal;
                result.ReasonIfNotEligible = $"Mua thêm {diff:N0} VNĐ để đủ điều kiện áp dụng mã này (Đơn tối thiểu {promo.MinOrderAmount:N0}đ).";
                return result;
            }

            // Tính số tiền được giảm
            double discount = 0;
            if (promo.percentDiscount > 0)
            {
                double percent = (double)promo.percentDiscount;
                if (percent > 1) percent = percent / 100.0;
                discount = applicableSubtotal * percent;

                if (promo.MaxDiscountAmount > 0 && discount > promo.MaxDiscountAmount)
                {
                    discount = promo.MaxDiscountAmount;
                }
            }
            else if (promo.DiscountValue > 0)
            {
                discount = (double)promo.DiscountValue;
            }

            if (discount > applicableSubtotal)
            {
                discount = applicableSubtotal;
            }

            result.IsEligible = true;
            result.CalculatedDiscount = discount;

            // Kiểm tra mã đã lưu trong ví chưa
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    result.IsSaved = _db.UserPromotion.Any(up => up.UserId == userId && up.PromotionId == promo.Id);
                }
                catch
                {
                    result.IsSaved = false;
                }
            }

            return result;
        }

        // Lấy danh sách mã giảm giá gợi ý cho giỏ hàng
        public List<VoucherEvaluationResult> GetSuggestedVouchersForCart(List<CartDetail> items, string userId = null)
        {
            SeedSampleVouchersIfEmpty();

            var now = DateTime.Now;
            var activePromos = _db.Promotion
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.EffectiveDate <= now && p.ExpiryDate >= now)
                .ToList();

            var results = new List<VoucherEvaluationResult>();
            foreach (var promo in activePromos)
            {
                results.Add(EvaluateVoucher(promo, items, userId));
            }

            // Ưu tiên xếp mã Đủ điều kiện và số tiền giảm cao nhất lên đầu
            return results.OrderByDescending(r => r.IsEligible)
                          .ThenByDescending(r => r.CalculatedDiscount)
                          .ThenBy(r => r.MinOrderAmount)
                          .ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminUserManagementService : IAdminUserManagementService
    {
        private readonly ApplicationDbContext _db;

        public AdminUserManagementService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
        }

        public async Task<(List<Order> Orders, ApplicationUser UserInfo, int TotalCount, int ProcessingCount, int CompletedCount, int CancelledCount, List<int> ReviewedProductIds)> GetUserOrdersDataAsync(string userId, string statusFilter)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            var ordersQuery = _db.Order
                .Include("OrderDetails.Product")
                .Where(o => o.UserId == userId);

            int totalCount = await _db.Order.CountAsync(o => o.UserId == userId);
            int processingCount = await _db.Order.CountAsync(o => o.UserId == userId && (o.OrderStatus == 0 || o.OrderStatus == 1 || o.OrderStatus == 2 || o.OrderStatus == 3));
            int completedCount = await _db.Order.CountAsync(o => o.UserId == userId && o.OrderStatus == 4);
            int cancelledCount = await _db.Order.CountAsync(o => o.UserId == userId && o.OrderStatus == 5);

            statusFilter = (statusFilter ?? "all").ToLower();

            if (statusFilter == "processing")
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == 0 || o.OrderStatus == 1 || o.OrderStatus == 2 || o.OrderStatus == 3);
            }
            else if (statusFilter == "completed")
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == 4);
            }
            else if (statusFilter == "cancelled")
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == 5);
            }

            var reviewedProductIds = new List<int>();
            try
            {
                var allReviews = await _db.Review.AsNoTracking().ToListAsync();
                reviewedProductIds = allReviews
                    .Where(r => r != null && r.UserId == userId)
                    .Select(r => r.ProductId)
                    .Distinct()
                    .ToList();
            }
            catch { }

            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
            return (orders, user, totalCount, processingCount, completedCount, cancelledCount, reviewedProductIds);
        }

        public async Task<(bool Success, string Message)> CancelUserOrderAsync(string userId, int orderId)
        {
            var order = await _db.Order.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order != null && order.OrderStatus == 0)
            {
                order.OrderStatus = 5; // 5 = Đã hủy
                await _db.SaveChangesAsync();

                try
                {
                    var walletSvc = new WalletService(_db);
                    await walletSvc.RefundOrderToWalletAsync(order.Id, "Khách hàng tự hủy đơn hàng");
                }
                catch { }

                return (true, $"Đã hủy thành công đơn hàng #{orderId}.");
            }
            return (false, "Không thể hủy đơn hàng này!");
        }

        public async Task<(List<UserPromotion> UserVouchers, ApplicationUser UserInfo, List<Promotion> AvailableToSave)> GetUserVouchersDataAsync(string userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var voucherService = new VoucherService(_db);
            voucherService.SeedSampleVouchersIfEmpty();

            var userVouchers = await _db.UserPromotion
                .Include(up => up.Promotion)
                .Include("Promotion.Category")
                .Where(up => up.UserId == userId)
                .OrderByDescending(up => up.SavedDate)
                .ToListAsync();

            var now = DateTime.Now;
            var allActivePromotions = await _db.Promotion
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.EffectiveDate <= now && p.ExpiryDate >= now)
                .ToListAsync();

            var savedIds = userVouchers.Select(uv => uv.PromotionId).ToList();
            var availableToSave = allActivePromotions.Where(p => !savedIds.Contains(p.Id)).ToList();

            return (userVouchers, user, availableToSave);
        }

        public async Task<(bool Success, string Message)> SaveVoucherForUserAsync(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return (false, "Vui lòng nhập mã giảm giá!");
            }

            var codeUpper = code.Trim().ToUpper();
            var now = DateTime.Now;
            var promo = await _db.Promotion.FirstOrDefaultAsync(p => p.Code.ToUpper() == codeUpper && p.IsActive && p.EffectiveDate <= now && p.ExpiryDate >= now);

            if (promo == null)
            {
                return (false, $"Mã giảm giá '{code}' không tồn tại hoặc đã hết hạn!");
            }

            bool alreadySaved = await _db.UserPromotion.AnyAsync(up => up.UserId == userId && up.PromotionId == promo.Id);
            if (alreadySaved)
            {
                return (false, $"Bạn đã lưu mã '{promo.Code}' vào Ví mã giảm giá trước đó rồi!");
            }

            _db.UserPromotion.Add(new UserPromotion
            {
                UserId = userId,
                PromotionId = promo.Id,
                IsUsed = false,
                SavedDate = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return (true, $"🎉 Đã lưu mã '{promo.Code}' vào Ví mã giảm giá cá nhân thành công!");
        }
    }
}

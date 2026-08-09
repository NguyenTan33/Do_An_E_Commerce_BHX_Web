using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminUserManagementService
    {
        Task<(List<Order> Orders, ApplicationUser UserInfo, int TotalCount, int ProcessingCount, int CompletedCount, int CancelledCount, List<int> ReviewedProductIds)> GetUserOrdersDataAsync(string userId, string statusFilter);
        Task<(bool Success, string Message)> CancelUserOrderAsync(string userId, int orderId);
        Task<(List<UserPromotion> UserVouchers, ApplicationUser UserInfo, List<Promotion> AvailableToSave)> GetUserVouchersDataAsync(string userId);
        Task<(bool Success, string Message)> SaveVoucherForUserAsync(string userId, string code);
    }
}

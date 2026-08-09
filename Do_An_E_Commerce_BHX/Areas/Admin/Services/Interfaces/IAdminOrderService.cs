using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminOrderService
    {
        Task<List<Order>> GetAdminOrdersAsync(string search, int? status);
        Task<Dictionary<string, int>> GetOrderCountsAsync();
        Task<bool> ApproveOrderAsync(int id);
        Task<bool> CancelOrderAsync(int id);
        Task<bool> DeleteOrderAsync(int id);

        Task<int> BulkApproveAsync(int[] ids);
        Task<int> BulkCancelAsync(int[] ids);
        Task<int> BulkDeleteAsync(int[] ids);

        Task<List<Order>> GetPackingListAsync(string search);
        Task<Order> GetOrderForPackingAsync(int id);
        Task<bool> CompletePackingAsync(int id, string currentUserId);

        Task<List<Order>> GetDeliveryListAsync(string search);
        Task<(bool Success, string StaffInfo)> StartDeliveryAsync(int id, string currentUserId, string note);
        Task<bool> CompleteDeliverySuccessAsync(int id, string currentUserId);
        Task<bool> CompleteDeliveryFailedAsync(int id);

        Task<(List<Order> Orders, double TotalSuccessRevenue, int TotalCount, int SuccessCount, int FailedCount)> GetOrderHistoryAsync(
            string search, int? status, decimal? minPrice, decimal? maxPrice, DateTime? fromDate, DateTime? toDate);

        Task<object> GetOrderDetailJsonDataAsync(int id);
    }
}

using System;
using System.Collections.Generic;

namespace Do_An_E_Commerce_BHX.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        // Thống kê tổng quan (All-time)
        public double TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockProducts { get; set; }

        // Thống kê theo kỳ lọc (Filtered)
        public double PeriodRevenue { get; set; } // Doanh thu trong kỳ lọc
        public int FilteredTotalOrders { get; set; }
        public int FilteredSuccessOrders { get; set; }
        public int FilteredPendingOrders { get; set; }
        public int FilteredCancelOrders { get; set; }

        // Dữ liệu hiển thị (Filtered)
        public List<TopProductVM> TopProducts { get; set; }
        public List<RevenueChartVM> RevenueChart { get; set; }
        public List<CategoryRevenueVM> CategoryRevenue { get; set; }

        // Thông tin bộ lọc hiện tại để View hiển thị
        public string CurrentPeriodInfo { get; set; }
    }

    public class TopProductVM
    {
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public double Revenue { get; set; }
    }

    public class RevenueChartVM
    {
        public string Label { get; set; }
        public double Revenue { get; set; }
    }

    public class CategoryRevenueVM
    {
        public string CategoryName { get; set; }
        public double Revenue { get; set; }
    }
}
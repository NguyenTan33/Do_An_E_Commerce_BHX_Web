using Do_An_E_Commerce_BHX.Areas.Admin.ViewModels;
using Do_An_E_Commerce_BHX.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext db;

        public DashboardService(ApplicationDbContext context)
        {
            db = context;
        }

        public DashboardViewModel GetDashboard(string period, DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var vm = new DashboardViewModel();
            var today = DateTime.Today;

            // 1. XỬ LÝ KHOẢNG THỜI GIAN LỌC (DATE RANGE)
            DateTime start = new DateTime(today.Year, today.Month, 1); // Mặc định là tháng này
            DateTime end = start.AddMonths(1).AddSeconds(-1);
            vm.CurrentPeriodInfo = "Tháng này";

            if (!string.IsNullOrEmpty(period))
            {
                switch (period.ToLower())
                {
                    case "today":
                        start = today;
                        end = today.AddDays(1).AddSeconds(-1);
                        vm.CurrentPeriodInfo = "Hôm nay";
                        break;
                    case "7days":
                        start = today.AddDays(-6);
                        end = today.AddDays(1).AddSeconds(-1);
                        vm.CurrentPeriodInfo = "7 ngày gần nhất";
                        break;
                    case "lastmonth":
                        start = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                        end = start.AddMonths(1).AddSeconds(-1);
                        vm.CurrentPeriodInfo = "Tháng trước";
                        break;
                    case "custom":
                        if (startDate.HasValue) start = startDate.Value.Date;
                        if (endDate.HasValue) end = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                        vm.CurrentPeriodInfo = $"Từ {start:dd/MM/yyyy} đến {end:dd/MM/yyyy}";
                        break;
                }
            }

            // 2. KHỞI TẠO CÁC QUERY THEO KỲ VÀ DANH MỤC
            // Query Đơn hàng
            var orderQuery = db.Order.Where(o => o.OrderDate >= start && o.OrderDate <= end);

            // Query Chi tiết đơn hàng (Dùng để tính doanh thu, biểu đồ, top SP) - Chỉ tính đơn đã giao thành công (Status = 3)
            var orderDetailQuery = db.OrderDetail
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.OrderStatus == 3 && od.Order.OrderDate >= start && od.Order.OrderDate <= end);

            // Nếu có lọc thêm theo Danh mục cụ thể
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                // Chỉ lấy những đơn hàng có mua sản phẩm thuộc danh mục này
                orderQuery = orderQuery.Where(o => o.OrderDetails.Any(od => od.Product.CategoryId == categoryId.Value));
                // Chỉ lấy chi tiết đơn của danh mục này để tính doanh thu
                orderDetailQuery = orderDetailQuery.Where(od => od.Product.CategoryId == categoryId.Value);
            }

            // 3. THỐNG KÊ TỔNG QUAN (Toàn thời gian)
            vm.TotalRevenue = db.Order.Where(x => x.OrderStatus == 3).Sum(x => (double?)x.TotalAmount) ?? 0;
            vm.TotalProducts = db.Product.Count();
            vm.TotalCategories = db.Category.Count();
            vm.TotalCustomers = db.Users.Count();
            vm.LowStockProducts = db.Product.Count(x => x.Quantity < 10);

            // 4. THỐNG KÊ THEO KỲ LỌC
            // Doanh thu trong kỳ (dựa vào chi tiết đơn để chính xác khi lọc theo danh mục)
            vm.PeriodRevenue = orderDetailQuery.Sum(x => (double?)(x.Quantity * x.Price)) ?? 0;

            vm.FilteredTotalOrders = orderQuery.Count();
            vm.FilteredSuccessOrders = orderQuery.Count(x => x.OrderStatus == 3);
            vm.FilteredPendingOrders = orderQuery.Count(x => x.OrderStatus == 0 || x.OrderStatus == 1 || x.OrderStatus == 2);
            vm.FilteredCancelOrders = orderQuery.Count(x => x.OrderStatus == 4);

            // 5. TOP SẢN PHẨM BÁN CHẠY (Theo kỳ lọc)
            vm.TopProducts = orderDetailQuery
                .GroupBy(x => new { x.ProductId, x.Product.Name })
                .Select(g => new TopProductVM
                {
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => (double)(x.Quantity * x.Price))
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(10)
                .ToList();

            // 6. TỶ TRỌNG DANH MỤC (Theo kỳ lọc)
            vm.CategoryRevenue = orderDetailQuery
                .GroupBy(x => x.Product.Category.Name)
                .Select(g => new CategoryRevenueVM
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(x => (double)(x.Quantity * x.Price))
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // 7. DỮ LIỆU BIỂU ĐỒ (Động: Nhóm theo ngày hoặc tháng tùy độ dài kỳ lọc)
            vm.RevenueChart = new List<RevenueChartVM>();
            int totalDays = (int)(end - start).TotalDays;

            if (totalDays <= 35) // Nếu lọc dưới 35 ngày -> Vẽ biểu đồ theo ngày
            {
                var groupedByDay = orderDetailQuery
                    .GroupBy(x => DbFunctions.TruncateTime(x.Order.OrderDate))
                    .Select(g => new { Date = g.Key.Value, Rev = g.Sum(x => (double)(x.Quantity * x.Price)) })
                    .ToList();

                for (int i = 0; i <= totalDays; i++)
                {
                    var d = start.AddDays(i).Date;
                    var rev = groupedByDay.FirstOrDefault(x => x.Date == d)?.Rev ?? 0;
                    vm.RevenueChart.Add(new RevenueChartVM { Label = d.ToString("dd/MM"), Revenue = rev });
                }
            }
            else // Nếu lọc khoảng thời gian quá dài -> Vẽ biểu đồ theo tháng
            {
                var groupedByMonth = orderDetailQuery
                    .GroupBy(x => new { x.Order.OrderDate.Year, x.Order.OrderDate.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Rev = g.Sum(x => (double)(x.Quantity * x.Price))
                    })
                    .ToList();

                var currentMonth = new DateTime(start.Year, start.Month, 1);
                while (currentMonth <= end)
                {
                    var rev = groupedByMonth.FirstOrDefault(x => x.Year == currentMonth.Year && x.Month == currentMonth.Month)?.Rev ?? 0;
                    vm.RevenueChart.Add(new RevenueChartVM { Label = $"T{currentMonth.Month}/{currentMonth.Year}", Revenue = rev });
                    currentMonth = currentMonth.AddMonths(1);
                }
            }

            return vm;
        }
    }
}
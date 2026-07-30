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

        public DashboardViewModel GetDashboard(string period, DateTime? startDate, DateTime? endDate, int? categoryId, int? status = null, int? paymentMethod = null)
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

            // 2. KHỞI TẠO CÁC QUERY THEO KỲ VÀ BỘ LỌC
            // Query Đơn hàng theo ngày
            var orderQuery = db.Order.Where(o => o.OrderDate >= start && o.OrderDate <= end);

            // Query Chi tiết đơn hàng (Thành công = OrderStatus 4, hoặc theo status lọc)
            int targetSuccessStatus = status.HasValue ? status.Value : 4; // Mặc định đơn thành công là 4
            var orderDetailQuery = db.OrderDetail
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.OrderDate >= start && od.Order.OrderDate <= end);

            // Nếu người dùng chọn Lọc Trạng thái cụ thể
            if (status.HasValue && status.Value >= 0)
            {
                orderQuery = orderQuery.Where(o => o.OrderStatus == status.Value);
                orderDetailQuery = orderDetailQuery.Where(od => od.Order.OrderStatus == status.Value);
            }
            else
            {
                // Mặc định tính doanh thu chi tiết từ các đơn THÀNH CÔNG (OrderStatus = 4)
                orderDetailQuery = orderDetailQuery.Where(od => od.Order.OrderStatus == 4);
            }

            // Nếu người dùng chọn Lọc Hình thức thanh toán (0 = COD, 1 = Ngân hàng, 2 = MoMo)
            if (paymentMethod.HasValue && paymentMethod.Value >= 0)
            {
                orderQuery = orderQuery.Where(o => o.PaymentMethod == paymentMethod.Value);
                orderDetailQuery = orderDetailQuery.Where(od => od.Order.PaymentMethod == paymentMethod.Value);
            }

            // Nếu người dùng chọn Lọc Danh mục sản phẩm
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                orderQuery = orderQuery.Where(o => o.OrderDetails.Any(od => od.Product.CategoryId == categoryId.Value));
                orderDetailQuery = orderDetailQuery.Where(od => od.Product.CategoryId == categoryId.Value);
            }

            // 3. THỐNG KÊ TỔNG QUAN (Toàn thời gian)
            vm.TotalRevenue = db.Order.Where(x => x.OrderStatus == 4).Sum(x => (double?)x.TotalAmount) ?? 0;
            vm.TotalProducts = db.Product.Count();
            vm.TotalCategories = db.Category.Count();
            vm.TotalCustomers = db.Users.Count();
            vm.LowStockProducts = db.Product.Count(x => x.Quantity < 10);

            // Thống kê lượt truy cập Website ngầm qua UserBehaviorLog
            try
            {
                vm.TotalPageViews = db.UserBehaviorLog.Count();
                vm.TotalUniqueVisitors = db.UserBehaviorLog.Select(x => x.SessionId).Distinct().Count();
                vm.TodayPageViews = db.UserBehaviorLog.Count(x => x.CreatedDate >= today);
                vm.PeriodPageViews = db.UserBehaviorLog.Count(x => x.CreatedDate >= start && x.CreatedDate <= end);
                vm.PeriodUniqueVisitors = db.UserBehaviorLog.Where(x => x.CreatedDate >= start && x.CreatedDate <= end).Select(x => x.SessionId).Distinct().Count();
            }
            catch
            {
                vm.TotalPageViews = 0;
                vm.TotalUniqueVisitors = 0;
                vm.TodayPageViews = 0;
                vm.PeriodPageViews = 0;
                vm.PeriodUniqueVisitors = 0;
            }

            // 4. THỐNG KÊ THEO KỲ LỌC
            vm.PeriodRevenue = orderDetailQuery.Sum(x => (double?)(x.Quantity * x.Price)) ?? 0;

            vm.FilteredTotalOrders = orderQuery.Count();
            vm.FilteredSuccessOrders = orderQuery.Count(x => x.OrderStatus == 4);
            vm.FilteredPendingOrders = orderQuery.Count(x => x.OrderStatus == 0 || x.OrderStatus == 1 || x.OrderStatus == 2 || x.OrderStatus == 3);
            vm.FilteredCancelOrders = orderQuery.Count(x => x.OrderStatus == 5);

            // 5. TOP SẢN PHẨM BÁN CHẠY
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

            // 6. TỶ TRỌNG DANH MỤC
            vm.CategoryRevenue = orderDetailQuery
                .GroupBy(x => x.Product.Category.Name)
                .Select(g => new CategoryRevenueVM
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(x => (double)(x.Quantity * x.Price))
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // 7. DỮ LIỆU BIỂU ĐỒ DOANH THU (Nhóm theo ngày hoặc tháng)
            vm.RevenueChart = new List<RevenueChartVM>();
            int totalDays = (int)(end - start).TotalDays;

            if (totalDays <= 35) // Vẽ biểu đồ theo ngày
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
            else // Vẽ biểu đồ theo tháng
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

            // 8. DANH SÁCH ĐƠN HÀNG TẠO NÊN DOANH THU KỲ LỌC
            vm.FilteredOrders = orderQuery
                .Include(o => o.User)
                .Include("OrderDetails.Product")
                .OrderByDescending(o => o.OrderDate)
                .Take(100)
                .ToList();

            return vm;
        }
    }
}
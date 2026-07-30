using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BehaviorAnalyticsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public BehaviorAnalyticsController()
        {
            ApplicationDbContext.EnsureProductColumnsExist(_db);
        }

        // ViewModels cho Báo cáo Thống kê Hành vi
        public class BehaviorAnalyticsViewModel
        {
            public int TotalEvents { get; set; }
            public int TotalSessions { get; set; }
            public int TotalPageViews { get; set; }        // Lượt xem trang theo kỳ lọc
            public int TotalUniqueVisitors { get; set; }   // Khách độc lập theo kỳ lọc
            public int TodayPageViews { get; set; }        // Lượt xem hôm nay
            public double AvgDwellSeconds { get; set; }
            public int TotalRageClicks { get; set; }

            public List<TopViewedProductDto> TopViewedProducts { get; set; } = new List<TopViewedProductDto>();
            public List<TopSearchKeywordDto> TopSearchKeywords { get; set; } = new List<TopSearchKeywordDto>();
            public FunnelAnalyticsDto Funnel { get; set; } = new FunnelAnalyticsDto();
            public DeviceDistributionDto Devices { get; set; } = new DeviceDistributionDto();
            public List<PageDwellDto> PageDwellTimes { get; set; } = new List<PageDwellDto>();
        }

        public class TopViewedProductDto
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int ViewCount { get; set; }
            public double AvgDurationSeconds { get; set; }
        }

        public class TopSearchKeywordDto
        {
            public string Keyword { get; set; }
            public int Count { get; set; }
        }

        public class FunnelAnalyticsDto
        {
            public int ProductViews { get; set; }
            public int CartAdditions { get; set; }
            public int CheckoutStarted { get; set; }
            public int OrdersCompleted { get; set; }
        }

        public class DeviceDistributionDto
        {
            public int MobileCount { get; set; }
            public int DesktopCount { get; set; }
            public int TabletCount { get; set; }
        }

        public class PageDwellDto
        {
            public string PagePath { get; set; }
            public int VisitCount { get; set; }
            public double AvgDurationSeconds { get; set; }
        }

        // GET: /Admin/BehaviorAnalytics
        public ActionResult Index(int? days, DateTime? startDate, DateTime? endDate)
        {
            var qLogs = _db.UserBehaviorLog.AsQueryable();
            var qOrders = _db.Order.AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                var start = startDate.Value.Date;
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                qLogs = qLogs.Where(l => l.CreatedDate >= start && l.CreatedDate <= end);
                qOrders = qOrders.Where(o => o.OrderDate >= start && o.OrderDate <= end);

                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
                ViewBag.SelectedDays = null;
            }
            else
            {
                int selectedDays = days.HasValue ? days.Value : 30;
                var cutoffDate = DateTime.Now.AddDays(-selectedDays);
                qLogs = qLogs.Where(l => l.CreatedDate >= cutoffDate);
                qOrders = qOrders.Where(o => o.OrderDate >= cutoffDate);

                ViewBag.SelectedDays = selectedDays;
                ViewBag.StartDate = null;
                ViewBag.EndDate = null;
            }

            var logs = qLogs.ToList();

            var today = DateTime.Today;
            int pageViewsCount = logs.Count(l => l.EventType == "PageView" || l.EventType == "PageLoadSpeed");
            if (pageViewsCount == 0) pageViewsCount = logs.Count;

            var vm = new BehaviorAnalyticsViewModel
            {
                TotalEvents = logs.Count,
                TotalSessions = logs.Select(l => l.SessionId).Distinct().Count(),
                TotalPageViews = pageViewsCount,
                TotalUniqueVisitors = logs.Select(l => l.SessionId).Distinct().Count(),
                TodayPageViews = _db.UserBehaviorLog.Count(l => l.CreatedDate >= today),
                TotalRageClicks = logs.Count(l => l.EventType == "RageClick")
            };

            // 1. Phân bổ Thiết bị
            vm.Devices.MobileCount = logs.Count(l => l.DeviceType == "Mobile");
            vm.Devices.DesktopCount = logs.Count(l => l.DeviceType == "Desktop");
            vm.Devices.TabletCount = logs.Count(l => l.DeviceType == "Tablet");

            // 2. Phễu chuyển đổi (Funnel)
            vm.Funnel.ProductViews = logs.Count(l => l.EventType == "ViewProduct");
            vm.Funnel.CartAdditions = logs.Count(l => l.EventType == "AddToCart");
            vm.Funnel.CheckoutStarted = logs.Count(l => l.EventType == "CheckoutStarted" || l.EventType == "CheckoutStep");
            vm.Funnel.OrdersCompleted = qOrders.Count(o => o.OrderStatus != 5);

            // 3. Top Sản phẩm xem nhiều & Thời gian xem
            var viewLogs = logs.Where(l => l.EventType == "ViewProduct" && l.TargetId.HasValue).ToList();
            vm.TopViewedProducts = viewLogs
                .GroupBy(l => l.TargetId.Value)
                .Select(g => new TopViewedProductDto
                {
                    ProductId = g.Key,
                    ProductName = g.FirstOrDefault()?.TargetName ?? ("Sản phẩm #" + g.Key),
                    ViewCount = g.Count(),
                    AvgDurationSeconds = g.Where(x => x.DurationSeconds.HasValue).Any() ? Math.Round(g.Where(x => x.DurationSeconds.HasValue).Average(x => x.DurationSeconds.Value), 1) : 0
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(10)
                .ToList();

            // 4. Top Từ khóa tìm kiếm
            var searchLogs = logs.Where(l => l.EventType == "SearchKeyword" && !string.IsNullOrEmpty(l.TargetName)).ToList();
            vm.TopSearchKeywords = searchLogs
                .GroupBy(l => l.TargetName.Trim().ToLower())
                .Select(g => new TopSearchKeywordDto
                {
                    Keyword = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // 5. Thời gian xem trung bình từng trang
            var dwellLogs = logs.Where(l => l.EventType == "PageDwellTime" && l.DurationSeconds.HasValue).ToList();
            if (dwellLogs.Any())
            {
                vm.AvgDwellSeconds = Math.Round(dwellLogs.Average(l => l.DurationSeconds.Value), 1);
            }

            vm.PageDwellTimes = dwellLogs
                .GroupBy(l => l.TargetName ?? "Trang khác")
                .Select(g => new PageDwellDto
                {
                    PagePath = g.Key,
                    VisitCount = g.Count(),
                    AvgDurationSeconds = Math.Round(g.Average(x => x.DurationSeconds.Value), 1)
                })
                .OrderByDescending(x => x.VisitCount)
                .Take(8)
                .ToList();

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class BehaviorAnalyticsController : AdminBaseController
    {
        private readonly IAnalyticsService _analyticsService;

        public BehaviorAnalyticsController()
        {
            ApplicationDbContext.EnsureProductColumnsExist(DbContext);
            _analyticsService = new AnalyticsService(DbContext);
        }

        public BehaviorAnalyticsController(IAnalyticsService analyticsService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _analyticsService = analyticsService ?? new AnalyticsService(DbContext);
        }

        // ViewModels cho Báo cáo Thống kê Hành vi
        public class BehaviorAnalyticsViewModel
        {
            public int TotalEvents { get; set; }
            public int TotalSessions { get; set; }
            public int TotalPageViews { get; set; }
            public int TotalUniqueVisitors { get; set; }
            public int TodayPageViews { get; set; }
            public double AvgDwellSeconds { get; set; }
            public int TotalRageClicks { get; set; }

            public List<TopViewedProductDto> TopViewedProducts { get; set; } = new List<TopViewedProductDto>();
            public List<TopSearchKeywordDto> TopSearchKeywords { get; set; } = new List<TopSearchKeywordDto>();
            public FunnelAnalyticsDto Funnel { get; set; } = new FunnelAnalyticsDto();
            public DeviceDistributionDto Devices { get; set; } = new DeviceDistributionDto();
            public List<PageDwellDto> PageDwellTimes { get; set; } = new List<PageDwellDto>();

            public int GuestSessionsCount { get; set; }
            public int RegisteredSessionsCount { get; set; }
            public List<GuestVisitorDto> RecentGuestVisitors { get; set; } = new List<GuestVisitorDto>();
        }

        public class GuestVisitorDto
        {
            public string SessionId { get; set; }
            public string IPAddress { get; set; }
            public string DeviceType { get; set; }
            public DateTime LastVisitDate { get; set; }
            public int PageViewCount { get; set; }
            public string ReferrerUrl { get; set; }
            public string UserFullName { get; set; }
            public bool IsGuest => string.IsNullOrEmpty(UserFullName);
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
        public async Task<ActionResult> Index(int? days, DateTime? startDate, DateTime? endDate)
        {
            await SetAdminFullNameViewBagAsync();

            if (startDate.HasValue && endDate.HasValue)
            {
                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
                ViewBag.SelectedDays = null;
            }
            else
            {
                int selectedDays = days.HasValue ? days.Value : 30;
                ViewBag.SelectedDays = selectedDays;
                ViewBag.StartDate = null;
                ViewBag.EndDate = null;
            }

            var vm = await _analyticsService.GetBehaviorAnalyticsAsync(days, startDate, endDate);
            return View(vm);
        }
    }
}

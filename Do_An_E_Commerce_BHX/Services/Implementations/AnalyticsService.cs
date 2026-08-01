using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Controllers;
using Do_An_E_Commerce_BHX.Controllers;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    /// <summary>
    /// Service xử lý ghi nhận log sự kiện hành vi người dùng (Behavior Analytics) và tổng hợp báo cáo cho Dashboard
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _db;

        public AnalyticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Ghi nhận một sự kiện hành vi người dùng (PageView, Click, AddToCart, DwellTime) vào cơ sở dữ liệu ngầm
        /// </summary>
        /// <param name="data">DTO chứa thông tin sự kiện gửi từ client</param>
        /// <param name="userId">ID người dùng (nếu đã đăng nhập)</param>
        /// <param name="userHostAddress">Địa chỉ IP của người dùng</param>
        /// <param name="userAgent">Chuỗi User-Agent của trình duyệt</param>
        /// <param name="sessionID">Mã Session định danh khách</param>
        /// <param name="urlReferrer">Trang web giới thiệu trước đó</param>
        public async Task LogBehaviorEventAsync(AnalyticsController.BehaviorEventDto data, string userId, string userHostAddress, string userAgent, string sessionID, Uri urlReferrer)
        {
            if (data == null || string.IsNullOrEmpty(data.EventType)) return;

            // Tự động phân loại thiết bị Mobile, Tablet hoặc Desktop dựa trên User-Agent
            string device = data.DeviceType;
            if (string.IsNullOrEmpty(device))
            {
                string ua = userAgent ?? "";
                if (ua.Contains("Mobile") || ua.Contains("Android") || ua.Contains("iPhone"))
                {
                    device = "Mobile";
                }
                else if (ua.Contains("iPad") || ua.Contains("Tablet"))
                {
                    device = "Tablet";
                }
                else
                {
                    device = "Desktop";
                }
            }

            var log = new UserBehaviorLog
            {
                SessionId = !string.IsNullOrEmpty(data.SessionId) ? data.SessionId : sessionID,
                UserId = userId,
                EventType = data.EventType,
                TargetId = data.TargetId,
                TargetName = data.TargetName,
                DurationSeconds = data.DurationSeconds,
                ScrollPercent = data.ScrollPercent,
                ReferrerUrl = !string.IsNullOrEmpty(data.ReferrerUrl) ? data.ReferrerUrl : urlReferrer?.ToString(),
                PageLoadMs = data.PageLoadMs,
                ExtraDataJson = data.ExtraDataJson,
                DeviceType = device,
                IPAddress = userHostAddress,
                CreatedDate = DateTime.Now
            };

            _db.UserBehaviorLog.Add(log);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Tổng hợp báo cáo Phân tích Hành vi (Behavior Analytics), Phễu chuyển đổi và Thống kê khách truy cập
        /// </summary>
        /// <param name="days">Số ngày lọc (30 ngày, 7 ngày...)</param>
        /// <param name="startDate">Ngày bắt đầu kỳ báo cáo</param>
        /// <param name="endDate">Ngày kết thúc kỳ báo cáo</param>
        public async Task<BehaviorAnalyticsController.BehaviorAnalyticsViewModel> GetBehaviorAnalyticsAsync(int? days, DateTime? startDate, DateTime? endDate)
        {
            var qLogs = _db.UserBehaviorLog.AsNoTracking().AsQueryable();
            var qOrders = _db.Order.AsNoTracking().AsQueryable();

            // Áp dụng bộ lọc thời gian từ ngày đến ngày hoặc theo khoảng số ngày
            if (startDate.HasValue && endDate.HasValue)
            {
                var start = startDate.Value.Date;
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                qLogs = qLogs.Where(l => l.CreatedDate >= start && l.CreatedDate <= end);
                qOrders = qOrders.Where(o => o.OrderDate >= start && o.OrderDate <= end);
            }
            else
            {
                int selectedDays = days.HasValue ? days.Value : 30;
                var cutoffDate = DateTime.Now.AddDays(-selectedDays);
                qLogs = qLogs.Where(l => l.CreatedDate >= cutoffDate);
                qOrders = qOrders.Where(o => o.OrderDate >= cutoffDate);
            }

            var logs = await qLogs.ToListAsync();
            var today = DateTime.Today;

            int pageViewsCount = logs.Count(l => l.EventType == "PageView" || l.EventType == "PageLoadSpeed");
            if (pageViewsCount == 0) pageViewsCount = logs.Count;

            var vm = new BehaviorAnalyticsController.BehaviorAnalyticsViewModel
            {
                TotalEvents = logs.Count,
                TotalSessions = logs.Select(l => l.SessionId).Distinct().Count(),
                TotalPageViews = pageViewsCount,
                TotalUniqueVisitors = logs.Select(l => l.SessionId).Distinct().Count(),
                TodayPageViews = await _db.UserBehaviorLog.AsNoTracking().CountAsync(l => l.CreatedDate >= today),
                TotalRageClicks = logs.Count(l => l.EventType == "RageClick")
            };

            // 1. Phân bổ Loại thiết bị (Mobile, Desktop, Tablet)
            vm.Devices.MobileCount = logs.Count(l => l.DeviceType == "Mobile");
            vm.Devices.DesktopCount = logs.Count(l => l.DeviceType == "Desktop");
            vm.Devices.TabletCount = logs.Count(l => l.DeviceType == "Tablet");

            // 2. Phễu chuyển đổi hành vi mua hàng (Funnel)
            vm.Funnel.ProductViews = logs.Count(l => l.EventType == "ViewProduct");
            vm.Funnel.CartAdditions = logs.Count(l => l.EventType == "AddToCart");
            vm.Funnel.CheckoutStarted = logs.Count(l => l.EventType == "CheckoutStarted" || l.EventType == "CheckoutStep");
            vm.Funnel.OrdersCompleted = await qOrders.CountAsync(o => o.OrderStatus != 5);

            // 3. Top Sản phẩm xem nhiều nhất và thời gian xem/đọc trung bình (Dwell Time)
            var productLogs = logs.Where(l => l.TargetId.HasValue || (l.TargetName != null && l.TargetName.IndexOf("/Product/Detail/", StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

            var productIds = productLogs
                .Select(l => {
                    if (l.TargetId.HasValue) return l.TargetId.Value;
                    if (!string.IsNullOrEmpty(l.TargetName))
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(l.TargetName, @"/Product/Detail/(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m.Success && int.TryParse(m.Groups[1].Value, out int pid)) return pid;
                    }
                    return 0;
                })
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var topViewedList = new List<BehaviorAnalyticsController.TopViewedProductDto>();

            foreach (var pid in productIds)
            {
                var pLogs = productLogs.Where(l => l.TargetId == pid || (l.TargetName != null && l.TargetName.IndexOf("/Product/Detail/" + pid, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

                int viewCount = pLogs.Count(l => l.EventType == "ViewProduct");
                if (viewCount == 0) viewCount = pLogs.Count(l => l.EventType == "PageView" || l.EventType == "PageDwellTime");

                var durationList = pLogs.Where(l => l.DurationSeconds.HasValue && l.DurationSeconds.Value > 0).Select(l => l.DurationSeconds.Value).ToList();
                double avgDwell = durationList.Any() ? Math.Round(durationList.Average(), 1) : 0;

                string pName = pLogs.FirstOrDefault(l => !string.IsNullOrEmpty(l.TargetName) && !l.TargetName.StartsWith("/"))?.TargetName;
                if (string.IsNullOrEmpty(pName))
                {
                    var dbProd = _db.Product.AsNoTracking().FirstOrDefault(p => p.Id == pid);
                    pName = dbProd != null ? dbProd.Name : ("Sản phẩm #" + pid);
                }

                topViewedList.Add(new BehaviorAnalyticsController.TopViewedProductDto
                {
                    ProductId = pid,
                    ProductName = pName,
                    ViewCount = viewCount,
                    AvgDurationSeconds = avgDwell
                });
            }

            vm.TopViewedProducts = topViewedList.OrderByDescending(x => x.ViewCount).Take(10).ToList();

            // 4. Top Từ khóa tìm kiếm nhiều nhất trên website
            var searchLogs = logs.Where(l => l.EventType == "SearchKeyword" && !string.IsNullOrEmpty(l.TargetName)).ToList();
            vm.TopSearchKeywords = searchLogs
                .GroupBy(l => l.TargetName.Trim().ToLower())
                .Select(g => new BehaviorAnalyticsController.TopSearchKeywordDto
                {
                    Keyword = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // 5. Thống kê thời gian dừng trung bình từng trang (DwellTime)
            var dwellLogs = logs.Where(l => l.EventType == "PageDwellTime" && !string.IsNullOrEmpty(l.TargetName) && l.DurationSeconds.HasValue).ToList();
            if (dwellLogs.Any())
            {
                vm.AvgDwellSeconds = Math.Round(dwellLogs.Average(l => l.DurationSeconds.Value), 1);
                vm.PageDwellTimes = dwellLogs
                    .GroupBy(l => l.TargetName)
                    .Select(g => new BehaviorAnalyticsController.PageDwellDto
                    {
                        PagePath = g.Key,
                        VisitCount = g.Count(),
                        AvgDurationSeconds = Math.Round(g.Average(x => x.DurationSeconds.Value), 1)
                    })
                    .OrderByDescending(x => x.VisitCount)
                    .Take(10)
                    .ToList();
            }

            // 6. Thống kê Khách Vãng Lai & Địa Chỉ IP Truy Cập
            var guestLogs = logs.Where(l => string.IsNullOrEmpty(l.UserId)).ToList();
            vm.GuestSessionsCount = guestLogs.Select(l => l.SessionId).Distinct().Count();
            vm.RegisteredSessionsCount = logs.Where(l => !string.IsNullOrEmpty(l.UserId)).Select(l => l.SessionId).Distinct().Count();

            var userMap = await _db.Users.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.FullName);

            vm.RecentGuestVisitors = logs
                .Where(l => !string.IsNullOrEmpty(l.SessionId))
                .GroupBy(l => l.SessionId)
                .Select(g => {
                    var lastLog = g.OrderByDescending(x => x.CreatedDate).First();
                    string uName = null;
                    if (!string.IsNullOrEmpty(lastLog.UserId) && userMap.ContainsKey(lastLog.UserId))
                    {
                        uName = userMap[lastLog.UserId];
                    }
                    return new BehaviorAnalyticsController.GuestVisitorDto
                    {
                        SessionId = g.Key,
                        IPAddress = string.IsNullOrEmpty(lastLog.IPAddress) ? "127.0.0.1" : lastLog.IPAddress,
                        DeviceType = string.IsNullOrEmpty(lastLog.DeviceType) ? "Desktop" : lastLog.DeviceType,
                        LastVisitDate = lastLog.CreatedDate,
                        PageViewCount = Math.Max(1, g.Count(x => x.EventType == "PageView" || x.EventType == "PageLoadSpeed")),
                        ReferrerUrl = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.ReferrerUrl))?.ReferrerUrl ?? "Trực tiếp (Direct)",
                        UserFullName = uName
                    };
                })
                .OrderByDescending(v => v.LastVisitDate)
                .Take(15)
                .ToList();

            return vm;
        }
    }
}

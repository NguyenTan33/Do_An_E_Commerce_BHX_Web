using System;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Controllers;
using Do_An_E_Commerce_BHX.Controllers;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IAnalyticsService
    {
        Task LogBehaviorEventAsync(AnalyticsController.BehaviorEventDto data, string userId, string userHostAddress, string userAgent, string sessionID, Uri urlReferrer);
        Task<BehaviorAnalyticsController.BehaviorAnalyticsViewModel> GetBehaviorAnalyticsAsync(int? days, DateTime? startDate, DateTime? endDate);
    }
}

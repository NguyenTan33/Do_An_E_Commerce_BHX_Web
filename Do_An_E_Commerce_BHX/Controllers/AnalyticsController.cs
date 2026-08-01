using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace Do_An_E_Commerce_BHX.Controllers
{
    [AllowAnonymous]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController()
        {
            ApplicationDbContext.EnsureProductColumnsExist(_db);
            _analyticsService = new AnalyticsService(_db);
        }

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        // DTO đại diện dữ liệu từ client
        public class BehaviorEventDto
        {
            public string SessionId { get; set; }
            public string EventType { get; set; }
            public int? TargetId { get; set; }
            public string TargetName { get; set; }
            public int? DurationSeconds { get; set; }
            public int? ScrollPercent { get; set; }
            public string ReferrerUrl { get; set; }
            public int? PageLoadMs { get; set; }
            public string ExtraDataJson { get; set; }
            public string DeviceType { get; set; }
        }

        // POST: /Analytics/LogEvent (Hỗ trợ AJAX & Beacon ngầm)
        [HttpPost]
        public async Task<ActionResult> LogEvent(BehaviorEventDto data)
        {
            try
            {
                // 1. Nếu Model Binder trả về null, đọc từ Form
                if (data == null || string.IsNullOrEmpty(data.EventType))
                {
                    data = new BehaviorEventDto();
                    data.SessionId = Request.Form["SessionId"];
                    data.EventType = Request.Form["EventType"];
                    if (int.TryParse(Request.Form["TargetId"], out int tid)) data.TargetId = tid;
                    data.TargetName = Request.Form["TargetName"];
                    if (int.TryParse(Request.Form["DurationSeconds"], out int ds)) data.DurationSeconds = ds;
                    if (int.TryParse(Request.Form["ScrollPercent"], out int sp)) data.ScrollPercent = sp;
                    data.ReferrerUrl = Request.Form["ReferrerUrl"];
                    data.ExtraDataJson = Request.Form["ExtraDataJson"];
                    data.DeviceType = Request.Form["DeviceType"];
                }

                // 2. Nếu vẫn null, đọc InputStream JSON Body
                if (string.IsNullOrEmpty(data.EventType) && Request.InputStream != null && Request.InputStream.Length > 0)
                {
                    try
                    {
                        Request.InputStream.Position = 0;
                        using (var reader = new StreamReader(Request.InputStream))
                        {
                            string body = await reader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(body))
                            {
                                var parsed = JsonConvert.DeserializeObject<BehaviorEventDto>(body);
                                if (parsed != null && !string.IsNullOrEmpty(parsed.EventType))
                                {
                                    data = parsed;
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (data == null || string.IsNullOrEmpty(data.EventType))
                {
                    return Json(new { success = false, message = "Invalid data" }, JsonRequestBehavior.AllowGet);
                }

                string userId = User != null && User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;

                if (!string.IsNullOrEmpty(userId))
                {
                    var userSvc = new UserService(_db, null, null);
                    await userSvc.UpdateLastActivityAsync(userId);
                }

                await _analyticsService.LogBehaviorEventAsync(
                    data,
                    userId,
                    Request.UserHostAddress,
                    Request.UserAgent,
                    Session.SessionID,
                    Request.UrlReferrer
                );

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

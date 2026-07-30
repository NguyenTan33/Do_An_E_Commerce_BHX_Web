using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace Do_An_E_Commerce_BHX.Controllers
{
    [AllowAnonymous]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public AnalyticsController()
        {
            ApplicationDbContext.EnsureProductColumnsExist(_db);
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
        public ActionResult LogEvent(BehaviorEventDto data)
        {
            try
            {
                // Nếu gửi bằng navigator.sendBeacon dữ liệu có thể nằm trong Request.InputStream
                if (data == null || string.IsNullOrEmpty(data.EventType))
                {
                    Request.InputStream.Position = 0;
                    using (var reader = new StreamReader(Request.InputStream))
                    {
                        string body = reader.ReadToEnd();
                        if (!string.IsNullOrEmpty(body))
                        {
                            data = JsonConvert.DeserializeObject<BehaviorEventDto>(body);
                        }
                    }
                }

                if (data == null || string.IsNullOrEmpty(data.EventType))
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                string userId = User != null && User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;
                string ip = Request.UserHostAddress;

                // Tự động phân loại thiết bị nếu không được truyền từ client
                string device = data.DeviceType;
                if (string.IsNullOrEmpty(device))
                {
                    string userAgent = Request.UserAgent ?? "";
                    if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
                    {
                        device = "Mobile";
                    }
                    else if (userAgent.Contains("iPad") || userAgent.Contains("Tablet"))
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
                    SessionId = !string.IsNullOrEmpty(data.SessionId) ? data.SessionId : Session.SessionID,
                    UserId = userId,
                    EventType = data.EventType,
                    TargetId = data.TargetId,
                    TargetName = data.TargetName,
                    DurationSeconds = data.DurationSeconds,
                    ScrollPercent = data.ScrollPercent,
                    ReferrerUrl = !string.IsNullOrEmpty(data.ReferrerUrl) ? data.ReferrerUrl : Request.UrlReferrer?.ToString(),
                    PageLoadMs = data.PageLoadMs,
                    ExtraDataJson = data.ExtraDataJson,
                    DeviceType = device,
                    IPAddress = ip,
                    CreatedDate = DateTime.Now
                };

                _db.UserBehaviorLog.Add(log);
                _db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}

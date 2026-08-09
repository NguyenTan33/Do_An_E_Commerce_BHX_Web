using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Controllers;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    [AllowAnonymous]
    public class HomeController : BaseController
    {
        private readonly IHomeService _homeService;

        public HomeController()
        {
            _homeService = new HomeService(DbContext);
        }

        public HomeController(IHomeService homeService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _homeService = homeService ?? new HomeService(DbContext);
        }

        public ActionResult Index(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortBy, int page = 1)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                try
                {
                    var analyticsService = new AnalyticsService(DbContext);
                    string currentUserId = User != null && User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;
                    string userIp = Request.UserHostAddress;
                    string userAgent = Request.UserAgent;
                    string sessionId = Session.SessionID;

                    var dto = new AnalyticsController.BehaviorEventDto
                    {
                        EventType = "SearchKeyword",
                        TargetName = searchTerm.Trim()
                    };

                    _ = Task.Run(() => analyticsService.LogBehaviorEventAsync(dto, currentUserId, userIp, userAgent, sessionId, Request.UrlReferrer));
                }
                catch { }
            }

            var model = _homeService.GetHomeIndexData(searchTerm, categoryId, minPrice, maxPrice, sortBy, page);
            return View(model);
        }

        [ChildActionOnly]
        public ActionResult SidebarCategories()
        {
            var categories = _homeService.GetSidebarCategories();
            return PartialView("_SidebarCategories", categories);
        }
    }
}
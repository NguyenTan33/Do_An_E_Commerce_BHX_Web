using Do_An_E_Commerce_BHX.Areas.Admin.Services;
using Do_An_E_Commerce_BHX.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // Nhận thêm tham số period, startDate, endDate, categoryId
        public ActionResult Index(string period, DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            ViewBag.FullName = user?.FullName;

            // Load danh sách Category để đổ ra View Dropdown
            ViewBag.CategoryId = new SelectList(db.Category.ToList(), "Id", "Name", categoryId);

            // Khởi tạo Service và truyền bộ lọc
            DashboardService service = new DashboardService(db);
            var model = service.GetDashboard(period, startDate, endDate, categoryId);

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
using Do_An_E_Commerce_BHX.Areas.Admin.Services;
using Do_An_E_Commerce_BHX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: /Admin/AdminDashboard
        public ActionResult Index(string period, DateTime? startDate, DateTime? endDate, int? categoryId, int? status, int? paymentMethod)
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            ViewBag.FullName = user?.FullName;

            // SelectList Danh mục
            ViewBag.CategoryId = new SelectList(db.Category.ToList(), "Id", "Name", categoryId);

            // SelectList Trạng thái đơn hàng
            var statusItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Tất cả trạng thái --" },
                new SelectListItem { Value = "0", Text = "0. Chờ duyệt", Selected = (status == 0) },
                new SelectListItem { Value = "1", Text = "1. Đã duyệt (Chờ soạn)", Selected = (status == 1) },
                new SelectListItem { Value = "2", Text = "2. Đã soạn xong", Selected = (status == 2) },
                new SelectListItem { Value = "3", Text = "3. Đang giao hàng", Selected = (status == 3) },
                new SelectListItem { Value = "4", Text = "4. Giao thành công (Mặc định)", Selected = (status == 4) },
                new SelectListItem { Value = "5", Text = "5. Đã hủy / Thất bại", Selected = (status == 5) }
            };
            ViewBag.StatusList = statusItems;

            // SelectList Hình thức thanh toán
            var paymentItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Tất cả hình thức thanh toán --" },
                new SelectListItem { Value = "0", Text = "💵 Tiền mặt khi nhận (COD)", Selected = (paymentMethod == 0) },
                new SelectListItem { Value = "1", Text = "🏦 Chuyển khoản VietQR", Selected = (paymentMethod == 1) },
                new SelectListItem { Value = "2", Text = "📱 Ví điện tử MoMo", Selected = (paymentMethod == 2) }
            };
            ViewBag.PaymentMethodList = paymentItems;

            ViewBag.SelectedPeriod = period;
            ViewBag.SelectedStartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedEndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedPaymentMethod = paymentMethod;

            // Khởi tạo DashboardService và nạp dữ liệu
            DashboardService service = new DashboardService(db);
            var model = service.GetDashboard(period, startDate, endDate, categoryId, status, paymentMethod);

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
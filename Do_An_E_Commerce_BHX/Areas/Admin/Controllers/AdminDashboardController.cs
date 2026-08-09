using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services;
using Do_An_E_Commerce_BHX.Models;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class AdminDashboardController : AdminBaseController
    {
        public AdminDashboardController()
        {
        }

        public AdminDashboardController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        // GET: /Admin/AdminDashboard
        public async Task<ActionResult> Index(string period, DateTime? startDate, DateTime? endDate, int? categoryId, int? status, int? paymentMethod)
        {
            await SetAdminFullNameViewBagAsync();

            var categories = DbContext.Category.ToList();
            ViewBag.CategoryId = new SelectList(categories, "Id", "Name", categoryId);

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

            DashboardService service = new DashboardService(DbContext);
            var model = service.GetDashboard(period, startDate, endDate, categoryId, status, paymentMethod);

            return View(model);
        }
    }
}
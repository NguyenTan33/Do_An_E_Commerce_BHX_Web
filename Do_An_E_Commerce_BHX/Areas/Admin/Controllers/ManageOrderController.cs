using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManageOrderController : AdminBaseController
    {
        private readonly IAdminOrderService _orderService;

        public ManageOrderController()
        {
            _orderService = new AdminOrderService(DbContext);
        }

        public ManageOrderController(IAdminOrderService orderService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _orderService = orderService ?? new AdminOrderService(DbContext);
        }

        // 1. GET: /Admin/ManageOrder (Mặc định chỉ hiện danh sách đơn CHƯA DUYỆT status = 0)
        public async Task<ActionResult> Index(string search = "", int? status = 0)
        {
            await SetAdminFullNameViewBagAsync();

            var listOrders = await _orderService.GetAdminOrdersAsync(search, status);
            var counts = await _orderService.GetOrderCountsAsync();

            ViewBag.CountAll = counts["CountAll"];
            ViewBag.CountPending = counts["CountPending"];
            ViewBag.CountApproved = counts["CountApproved"];
            ViewBag.CountPacked = counts["CountPacked"];
            ViewBag.CountDelivering = counts["CountDelivering"];
            ViewBag.CountSuccess = counts["CountSuccess"];
            ViewBag.CountFailed = counts["CountFailed"];

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;

            return View(listOrders);
        }

        // 2. POST: /Admin/ManageOrder/Approve (Duyệt đơn lẻ)
        [HttpPost]
        public async Task<ActionResult> Approve(int id)
        {
            bool success = await _orderService.ApproveOrderAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            return Json(new { success = true, message = $"Đã duyệt đơn hàng #{id} thành công!" });
        }

        // 3. POST: /Admin/ManageOrder/Cancel (Hủy đơn lẻ + Hoàn trả tồn kho)
        [HttpPost]
        public async Task<ActionResult> Cancel(int id)
        {
            bool success = await _orderService.CancelOrderAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            return Json(new { success = true, message = $"Đã hủy đơn hàng #{id} và hoàn trả lại tồn kho!" });
        }

        // 4. POST: /Admin/ManageOrder/Delete (Xóa đơn lẻ)
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            bool success = await _orderService.DeleteOrderAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            return Json(new { success = true, message = $"Đã xóa đơn hàng #{id} thành công!" });
        }

        // 5. POST: /Admin/ManageOrder/BulkApprove (Duyệt tất cả các đơn đã chọn)
        [HttpPost]
        public async Task<ActionResult> BulkApprove(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 đơn hàng để duyệt!" });
            }

            int count = await _orderService.BulkApproveAsync(ids);
            return Json(new { success = true, message = $"Đã duyệt hàng loạt {count} đơn hàng!" });
        }

        // 6. POST: /Admin/ManageOrder/BulkCancel (Hủy tất cả các đơn đã chọn + Hoàn trả tồn kho)
        [HttpPost]
        public async Task<ActionResult> BulkCancel(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 đơn hàng để hủy!" });
            }

            int count = await _orderService.BulkCancelAsync(ids);
            return Json(new { success = true, message = $"Đã hủy {count} đơn hàng và cộng trả lại tồn kho!" });
        }

        // 7. POST: /Admin/ManageOrder/BulkDelete (Xóa tất cả các đơn đã chọn)
        [HttpPost]
        public async Task<ActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 đơn hàng để xóa!" });
            }

            int count = await _orderService.BulkDeleteAsync(ids);
            return Json(new { success = true, message = $"Đã xóa {count} đơn hàng thành công!" });
        }

        // 8. GET: /Admin/ManageOrder/PackingList (Danh sách đơn hàng chờ soạn - OrderStatus = 1)
        public async Task<ActionResult> PackingList(string search = "")
        {
            await SetAdminFullNameViewBagAsync();
            var list = await _orderService.GetPackingListAsync(search);
            ViewBag.CurrentSearch = search;
            return View(list);
        }

        // 9. GET: /Admin/ManageOrder/PackingDetail/123 (Giao diện soạn đơn chi tiết tích mặt hàng)
        public async Task<ActionResult> PackingDetail(int id)
        {
            var order = await _orderService.GetOrderForPackingAsync(id);
            if (order == null)
            {
                return RedirectToAction("PackingList");
            }

            return View(order);
        }

        // 10. POST: /Admin/ManageOrder/CompletePacking (Hoàn tất soạn đơn -> Chuyển sang chờ giao hàng OrderStatus = 2)
        [HttpPost]
        public async Task<ActionResult> CompletePacking(int id)
        {
            string currentUserId = User.Identity.GetUserId();
            bool success = await _orderService.CompletePackingAsync(id, currentUserId);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            return Json(new { 
                success = true, 
                message = $"Đã soạn xong đơn hàng #{id}! Chuyển sang danh sách giao hàng.",
                redirectUrl = Url.Action("DeliveryList", "ManageOrder")
            });
        }

        // 11. GET: /Admin/ManageOrder/DeliveryList (Trang giao hàng & phân công Shipper - OrderStatus = 2 hoặc 3)
        public async Task<ActionResult> DeliveryList(string search = "")
        {
            await SetAdminFullNameViewBagAsync();
            var list = await _orderService.GetDeliveryListAsync(search);
            ViewBag.CurrentSearch = search;
            return View(list);
        }

        // 12. POST: /Admin/ManageOrder/StartDelivery (Bắt đầu giao hàng - OrderStatus = 3)
        [HttpPost]
        public async Task<ActionResult> StartDelivery(int id, string note = "")
        {
            string currentUserId = User.Identity.GetUserId();
            var (success, staffInfo) = await _orderService.StartDeliveryAsync(id, currentUserId, note);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            return Json(new { success = true, message = $"Đơn hàng #{id} đã được nhận giao thành công bởi Shipper ({staffInfo})!" });
        }

        // 13. POST: /Admin/ManageOrder/CompleteDeliverySuccess (Giao hàng THÀNH CÔNG -> OrderStatus = 4, PaymentStatus = 1)
        [HttpPost]
        public async Task<ActionResult> CompleteDeliverySuccess(int id)
        {
            string currentUserId = User.Identity.GetUserId();
            bool success = await _orderService.CompleteDeliverySuccessAsync(id, currentUserId);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            return Json(new { success = true, message = $"Đã cập nhật đơn hàng #{id} GIAO HÀNG THÀNH CÔNG!" });
        }

        // 14. POST: /Admin/ManageOrder/CompleteDeliveryFailed (Giao hàng THẤT BẠI -> OrderStatus = 5 & Hoàn tồn kho)
        [HttpPost]
        public async Task<ActionResult> CompleteDeliveryFailed(int id)
        {
            bool success = await _orderService.CompleteDeliveryFailedAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            return Json(new { success = true, message = $"Đã cập nhật đơn hàng #{id} GIAO THẤT BẠI và HOÀN LẠI TỒN KHO sản phẩm!" });
        }

        // 15. GET: /Admin/ManageOrder/History (Trang lịch sử đơn hàng với bộ lọc tìm kiếm nâng cao)
        public async Task<ActionResult> History(string search = "", int? status = null, decimal? minPrice = null, decimal? maxPrice = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            await SetAdminFullNameViewBagAsync();

            var (listOrders, totalRevenue, totalCount, successCount, failedCount) = await _orderService.GetOrderHistoryAsync(
                search, status, minPrice, maxPrice, fromDate, toDate);

            ViewBag.TotalSuccessRevenue = totalRevenue;
            ViewBag.TotalHistoryCount = totalCount;
            ViewBag.CountSuccess = successCount;
            ViewBag.CountFailed = failedCount;

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(listOrders);
        }

        // 16. GET: /Admin/ManageOrder/GetOrderDetailJson?id=123 (Lấy JSON chi tiết đơn hàng cho Modal Xem Chi Tiết)
        [HttpGet]
        public async Task<ActionResult> GetOrderDetailJson(int id)
        {
            var data = await _orderService.GetOrderDetailJsonDataAsync(id);
            if (data == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
        }
    }
}

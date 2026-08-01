using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageOrderController : Controller
    {
        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private readonly OrderService _orderService;

        public ManageOrderController()
        {
            var calc = new Calculate();
            var cartSvc = new CartService(_dbContext);
            _orderService = new OrderService(_dbContext, calc, cartSvc);
        }

        // 1. GET: /Admin/ManageOrder (Mặc định chỉ hiện danh sách đơn CHƯA DUYỆT status = 0)
        public async Task<ActionResult> Index(string search = "", int? status = 0)
        {
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
        public ActionResult BulkApprove(int[] ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 đơn hàng để duyệt!" });
            }

            var orders = _dbContext.Order.Where(o => ids.Contains(o.Id) && o.OrderStatus == 0).ToList();
            foreach (var o in orders)
            {
                o.OrderStatus = 1; // Đã duyệt
            }
            _dbContext.SaveChanges();

            return Json(new { success = true, message = $"Đã duyệt hàng loạt {orders.Count} đơn hàng!" });
        }

        // 6. POST: /Admin/ManageOrder/BulkCancel (Hủy tất cả các đơn đã chọn + Hoàn trả tồn kho)
        [HttpPost]
        public ActionResult BulkCancel(int[] ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 đơn hàng để hủy!" });
            }

            var orders = _dbContext.Order.Include("OrderDetails").Where(o => ids.Contains(o.Id)).ToList();
            int count = 0;
            foreach (var o in orders)
            {
                if (o.OrderStatus != 5)
                {
                    RestoreOrderStock(o);
                    o.OrderStatus = 5; // Hủy
                    count++;
                }
            }
            _dbContext.SaveChanges();

            return Json(new { success = true, message = $"Đã hủy {count} đơn hàng và cộng trả lại tồn kho!" });
        }

        // 7. POST: /Admin/ManageOrder/BulkDelete (Xóa tất cả các đơn đã chọn)
        [HttpPost]
        public ActionResult BulkDelete(int[] ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 đơn hàng để xóa!" });
            }

            var orders = _dbContext.Order.Include("OrderDetails").Where(o => ids.Contains(o.Id)).ToList();
            foreach (var o in orders)
            {
                if (o.OrderStatus != 5 && o.OrderStatus != 4)
                {
                    RestoreOrderStock(o);
                }
                _dbContext.OrderDetail.RemoveRange(o.OrderDetails);
                _dbContext.Order.Remove(o);
            }
            _dbContext.SaveChanges();

            return Json(new { success = true, message = $"Đã xóa {orders.Count} đơn hàng thành công!" });
        }

        // 8. GET: /Admin/ManageOrder/PackingList (Danh sách đơn hàng chờ soạn - OrderStatus = 1)
        public ActionResult PackingList(string search = "")
        {
            var query = _dbContext.Order
                .Include("OrderDetails.Product")
                .Where(o => o.OrderStatus == 1);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                int idSearch;
                bool isNum = int.TryParse(s, out idSearch);
                query = query.Where(o => o.ReceiverPhone.Contains(s) || (isNum && o.Id == idSearch));
            }

            var list = query.OrderByDescending(o => o.OrderDate).ToList();
            ViewBag.CurrentSearch = search;
            return View(list);
        }

        // 9. GET: /Admin/ManageOrder/PackingDetail/123 (Giao diện soạn đơn chi tiết tích mặt hàng)
        public ActionResult PackingDetail(int id)
        {
            var order = _dbContext.Order
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return RedirectToAction("PackingList");
            }

            return View(order);
        }

        // 10. POST: /Admin/ManageOrder/CompletePacking (Hoàn tất soạn đơn -> Chuyển sang chờ giao hàng OrderStatus = 2)
        [HttpPost]
        public ActionResult CompletePacking(int id)
        {
            var order = _dbContext.Order.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            string currentUserId = User.Identity.GetUserId();
            var currentUser = _dbContext.Users.FirstOrDefault(u => u.Id == currentUserId);
            string staffInfo = currentUser != null ? (currentUser.FullName ?? currentUser.UserName) : "Nhân viên Admin";

            order.OrderStatus = 2; // Đã soạn xong / Chờ giao hàng
            order.Note = (order.Note ?? "") + $" [Soạn bởi: {staffInfo}]";
            _dbContext.SaveChanges();

            return Json(new { 
                success = true, 
                message = $"Đã soạn xong đơn hàng #{id}! Chuyển sang danh sách giao hàng.",
                redirectUrl = Url.Action("DeliveryList", "ManageOrder")
            });
        }

        // 11. GET: /Admin/ManageOrder/DeliveryList (Trang giao hàng & phân công Shipper - OrderStatus = 2 hoặc 3)
        public ActionResult DeliveryList(string search = "")
        {
            var query = _dbContext.Order
                .Include("OrderDetails.Product")
                .Where(o => o.OrderStatus == 2 || o.OrderStatus == 3);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                int idSearch;
                bool isNum = int.TryParse(s, out idSearch);
                query = query.Where(o => o.ReceiverPhone.Contains(s) || (isNum && o.Id == idSearch));
            }

            var list = query.OrderByDescending(o => o.OrderDate).ToList();
            ViewBag.CurrentSearch = search;
            return View(list);
        }

        // 12. POST: /Admin/ManageOrder/StartDelivery (Bắt đầu giao hàng - OrderStatus = 3)
        [HttpPost]
        public ActionResult StartDelivery(int id, string note = "")
        {
            var order = _dbContext.Order.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            string currentUserId = User.Identity.GetUserId();
            var currentUser = _dbContext.Users.FirstOrDefault(u => u.Id == currentUserId);
            string staffInfo = currentUser != null ? (currentUser.FullName ?? currentUser.UserName) : "Shipper";

            if (!string.IsNullOrEmpty(currentUserId))
            {
                order.UserId = currentUserId;
            }

            order.OrderStatus = 3; // Đang giao hàng
            order.Note = (order.Note ?? "") + $" [Giao bởi: {staffInfo}" + (!string.IsNullOrWhiteSpace(note) ? $" - {note.Trim()}" : "") + "]";
            _dbContext.SaveChanges();

            return Json(new { success = true, message = $"Đơn hàng #{id} đã được nhận giao thành công bởi Shipper ({staffInfo})!" });
        }

        // 13. POST: /Admin/ManageOrder/CompleteDeliverySuccess (Giao hàng THÀNH CÔNG -> OrderStatus = 4, PaymentStatus = 1)
        [HttpPost]
        public ActionResult CompleteDeliverySuccess(int id)
        {
            var order = _dbContext.Order.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            string currentUserId = User.Identity.GetUserId();
            var currentUser = _dbContext.Users.FirstOrDefault(u => u.Id == currentUserId);
            string staffInfo = currentUser != null ? (!string.IsNullOrEmpty(currentUser.FullName) ? $"{currentUser.FullName} ({currentUser.UserName})" : currentUser.UserName) : "Shipper Bách Hóa Xanh";

            order.OrderStatus = 4;   // Thành công / Hoàn tất
            order.PaymentStatus = 1; // Đã thanh toán

            if (string.IsNullOrEmpty(order.Note) || !order.Note.Contains("Giao bởi:"))
            {
                order.Note = (order.Note ?? "") + $" [Giao bởi: {staffInfo}]";
            }

            _dbContext.SaveChanges();

            return Json(new { success = true, message = $"Đã cập nhật đơn hàng #{id} GIAO HÀNG THÀNH CÔNG!" });
        }

        // 14. POST: /Admin/ManageOrder/CompleteDeliveryFailed (Giao hàng THẤT BẠI -> OrderStatus = 5 & Hoàn tồn kho)
        [HttpPost]
        public ActionResult CompleteDeliveryFailed(int id)
        {
            var order = _dbContext.Order.Include("OrderDetails").FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            if (order.OrderStatus != 5)
            {
                RestoreOrderStock(order);
                order.OrderStatus = 5; // Giao thất bại / Hủy đơn
                _dbContext.SaveChanges();
            }

            return Json(new { success = true, message = $"Đã cập nhật đơn hàng #{id} GIAO THẤT BẠI và HOÀN LẠI TỒN KHO sản phẩm!" });
        }

        // 15. GET: /Admin/ManageOrder/History (Trang lịch sử đơn hàng với bộ lọc tìm kiếm nâng cao)
        public ActionResult History(string search = "", int? status = null, decimal? minPrice = null, decimal? maxPrice = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _dbContext.Order
                .Include(o => o.User)
                .Include("OrderDetails.Product")
                .AsQueryable();

            // 1. Lọc văn bản (Mã đơn, SĐT, Tên khách)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                int orderIdSearch;
                bool isNumeric = int.TryParse(s, out orderIdSearch);
                query = query.Where(o => o.ReceiverPhone.Contains(s) || 
                                         o.ReceiverName.Contains(s) || 
                                         (isNumeric && o.Id == orderIdSearch));
            }

            // 2. Lọc trạng thái đơn
            if (status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            // 3. Lọc khoảng giá (minPrice, maxPrice)
            if (minPrice.HasValue)
            {
                double minD = (double)minPrice.Value;
                query = query.Where(o => o.TotalAmount >= minD);
            }
            if (maxPrice.HasValue)
            {
                double maxD = (double)maxPrice.Value;
                query = query.Where(o => o.TotalAmount <= maxD);
            }

            // 4. Lọc khoảng ngày (fromDate, toDate)
            if (fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                query = query.Where(o => o.OrderDate >= start);
            }
            if (toDate.HasValue)
            {
                DateTime end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.OrderDate <= end);
            }

            var listOrders = query.OrderByDescending(o => o.OrderDate).ToList();

            // Thống kê báo cáo
            double totalSuccessRevenue = listOrders.Where(o => o.OrderStatus == 4).Sum(o => o.TotalAmount);
            ViewBag.TotalSuccessRevenue = totalSuccessRevenue;
            ViewBag.TotalHistoryCount = listOrders.Count;
            ViewBag.CountSuccess = listOrders.Count(o => o.OrderStatus == 4);
            ViewBag.CountFailed = listOrders.Count(o => o.OrderStatus == 5);

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
        public ActionResult GetOrderDetailJson(int id)
        {
            var order = _dbContext.Order
                .Include(o => o.User)
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" }, JsonRequestBehavior.AllowGet);
            }

            var items = order.OrderDetails.Select(od => new
            {
                productId = od.ProductId,
                productName = od.Product != null ? od.Product.Name : "Sản phẩm",
                barcode = od.Product != null ? od.Product.Barcode : "",
                image = od.Product != null ? od.Product.URLImage : "",
                quantity = od.Quantity,
                price = od.Price,
                total = od.Price * od.Quantity
            }).ToList();

            string noteStr = order.Note ?? "";
            string packedBy = ExtractNoteTag(noteStr, "Soạn bởi:") ?? ExtractNoteTag(noteStr, "Soạn hàng bởi:");
            string deliveredBy = ExtractNoteTag(noteStr, "Giao bởi:") ?? ExtractNoteTag(noteStr, "Giao hàng bởi:") ?? ExtractNoteTag(noteStr, "Shipper:");

            // Xử lý thông minh cho các đơn hàng đã soạn / đã giao từ trước
            if (string.IsNullOrWhiteSpace(packedBy))
            {
                if (order.OrderStatus >= 2) // Đã soạn, Đang giao hoặc Thành công
                {
                    packedBy = "Nhân viên Bách Hóa Xanh (Admin)";
                }
                else
                {
                    packedBy = "Chưa thực hiện soạn đơn";
                }
            }

            if (string.IsNullOrWhiteSpace(deliveredBy))
            {
                if (order.OrderStatus == 3 || order.OrderStatus == 4) // Đang giao hoặc Thành công
                {
                    if (order.User != null)
                    {
                        deliveredBy = !string.IsNullOrEmpty(order.User.FullName) ? $"{order.User.FullName} ({order.User.UserName})" : order.User.UserName;
                    }
                    else
                    {
                        deliveredBy = "Shipper Bách Hóa Xanh";
                    }
                }
                else
                {
                    deliveredBy = "Chưa thực hiện giao hàng";
                }
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    id = order.Id,
                    orderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    receiverName = order.ReceiverName,
                    receiverPhone = order.ReceiverPhone,
                    shippingAddress = order.ShippingAddress,
                    note = noteStr,
                    packedBy = packedBy,
                    deliveredBy = deliveredBy,
                    paymentMethod = order.PaymentMethod == 1 ? "Ngân hàng (VietQR)" : (order.PaymentMethod == 2 ? "Ví MoMo" : "Tiền mặt (COD)"),
                    paymentStatus = order.PaymentStatus == 1 ? "Đã thanh toán" : "Chưa thanh toán",
                    orderStatus = order.OrderStatus,
                    totalAmount = order.TotalAmount,
                    discountAmount = order.DiscountAmount,
                    shippingFee = order.ShippingFee,
                    items = items
                }
            }, JsonRequestBehavior.AllowGet);
        }

        // Helper trích xuất nhãn ghi vết nhân viên từ Note
        private string ExtractNoteTag(string note, string tagPrefix)
        {
            if (string.IsNullOrEmpty(note)) return null;
            int idx = note.IndexOf(tagPrefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int start = idx + tagPrefix.Length;
            int end = note.IndexOf("]", start);
            if (end > start)
            {
                return note.Substring(start, end - start).Trim(' ', ':');
            }
            return note.Substring(start).Trim(' ', ':');
        }

        // Helper private method: Hoàn trả số lượng tồn kho sản phẩm cho đơn bị hủy/thất bại
        private void RestoreOrderStock(Order order)
        {
            if (order == null || order.OrderDetails == null) return;

            foreach (var item in order.OrderDetails)
            {
                var product = _dbContext.Product.Find(item.ProductId);
                if (product != null)
                {
                    // Lấy sản phẩm thực tế cần trả kho (Nếu là bài quy cách con Thùng/Lốc, trả kho về bài Lon lẻ gốc)
                    var targetStockProduct = (product.ParentProductId.HasValue && product.ParentProductId.Value > 0)
                        ? _dbContext.Product.Find(product.ParentProductId.Value) ?? product
                        : product;

                    int factor = product.UnitMultiplier > 0 ? product.UnitMultiplier : 1;
                    int quantityToRestore = item.Quantity * factor;

                    targetStockProduct.Quantity += quantityToRestore;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}

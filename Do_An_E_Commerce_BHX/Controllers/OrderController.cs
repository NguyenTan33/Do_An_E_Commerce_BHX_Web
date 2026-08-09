using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class PendingCheckoutSession
    {
        public string PendingCode { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ShippingAddress { get; set; }
        public int PaymentMethod { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public int UsedPoints { get; set; }
        public string Note { get; set; }
        public string SelectedIds { get; set; }
        public string CouponCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class OrderController : BaseController
    {
        private readonly IOrderCheckoutService _checkoutService;

        public OrderController()
        {
            _checkoutService = new OrderCheckoutService(DbContext);
        }

        public OrderController(IOrderCheckoutService checkoutService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _checkoutService = checkoutService ?? new OrderCheckoutService(DbContext);
        }

        // GET: /Order/Index (Trang Thanh Toán)
        [HttpGet]
        public async Task<ActionResult> Index(string selectedIds = null, string coupon = null)
        {
            string userId = GetCurrentUserId();
            var (cart, userAddresses, userFullName, userPhone, loyaltyPoints, suggestedVouchers, discountAmount, appliedCode, couponMessage) =
                await _checkoutService.GetCheckoutDataAsync(userId, selectedIds, coupon);

            if (cart == null)
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.UserAddresses = userAddresses;
            ViewBag.UserFullName = userFullName;
            ViewBag.UserPhone = userPhone;
            ViewBag.LoyaltyPoints = loyaltyPoints;
            ViewBag.SuggestedVouchers = suggestedVouchers;
            ViewBag.CouponCode = appliedCode;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.CouponMessage = couponMessage;
            ViewBag.SelectedIds = selectedIds ?? "";

            return View(cart);
        }

        // POST: /Order/Checkout (Xử lý chốt đơn qua AJAX)
        [HttpPost]
        public ActionResult Checkout(string receiverName, string receiverPhone, string shippingAddress, int paymentMethod = 0, decimal shippingFee = 0, decimal discountAmount = 0, int usedPoints = 0, string note = "", string selectedIds = "", string couponCode = "")
        {
            string userId = GetCurrentUserId();

            try
            {
                var (success, message, orderId, isPendingSession, pendingSession) = _checkoutService.CreatePendingCheckoutSession(
                    userId, receiverName, receiverPhone, shippingAddress, paymentMethod, shippingFee, discountAmount, usedPoints, note, selectedIds, couponCode);

                if (!success)
                {
                    return Json(new { success = false, message = message });
                }

                Session["PendingCheckoutSession"] = pendingSession;

                return Json(new {
                    success = true,
                    message = message,
                    orderId = 0,
                    isPendingSession = true,
                    redirectUrl = Url.Action("Payment", "Order", new { isPending = 1 })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "LỖI ĐẶT HÀNG: " + ex.Message });
            }
        }

        // GET: /Order/Payment?orderId=123 hoặc ?isPending=1 (Trang quét mã VietQR)
        [HttpGet]
        public ActionResult Payment(int orderId = 0, int isPending = 0)
        {
            if (orderId > 0)
            {
                var order = DbContext.Order
                    .Include("OrderDetails.Product")
                    .FirstOrDefault(o => o.Id == orderId);

                if (order != null)
                {
                    return View(order);
                }
            }

            var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
            if (pending != null)
            {
                string userId = GetCurrentUserId();
                var pendingOrderModel = _checkoutService.GetPendingOrderForPaymentView(pending, userId);
                if (pendingOrderModel != null)
                {
                    return View(pendingOrderModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Order/ProcessCOD (Xử lý hoàn tất đặt hàng COD)
        [HttpPost]
        public ActionResult ProcessCOD(int orderId = 0)
        {
            try
            {
                string userId = GetCurrentUserId();
                var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;

                var (success, message, createdOrderId) = _checkoutService.ProcessCODCheckout(userId, orderId, pending);
                if (success)
                {
                    if (pending != null) Session.Remove("PendingCheckoutSession");
                    return Json(new {
                        success = true,
                        message = message,
                        redirectUrl = Url.Action("Success", "Order", new { orderId = createdOrderId, paymentMethod = 0 })
                    });
                }

                return Json(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xử lý COD: " + ex.Message });
            }
        }

        // POST: /Order/ConfirmBankPaymentApi (Xác nhận thanh toán chuyển khoản / MoMo thành công)
        [HttpPost]
        public ActionResult ConfirmBankPaymentApi(int orderId = 0, int paymentMethod = 1)
        {
            try
            {
                string userId = GetCurrentUserId();
                var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;

                var (success, isPaid, message, createdOrderId) = _checkoutService.ConfirmBankPayment(userId, orderId, paymentMethod, pending);
                if (success)
                {
                    if (pending != null)
                    {
                        Session.Remove("PendingCheckoutSession");
                        Session["LastCreatedOrderId"] = createdOrderId;
                    }

                    return Json(new {
                        isPaid = true,
                        success = true,
                        message = message,
                        orderId = createdOrderId,
                        redirectUrl = Url.Action("Success", "Order", new { orderId = createdOrderId, paymentMethod = paymentMethod })
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = false, message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xác nhận thanh toán: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: /Order/Success (Trang thông báo hoàn tất)
        [HttpGet]
        public ActionResult Success(int paymentMethod = 0, int orderId = 0)
        {
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.OrderId = orderId;

            if (orderId > 0)
            {
                var order = DbContext.Order
                    .Include("OrderDetails.Product")
                    .FirstOrDefault(o => o.Id == orderId);
                ViewBag.Order = order;
            }

            return View();
        }

        // GET: /Order/CheckPaymentStatus?orderId=123 (Auto-Polling gọi trực tiếp SePay REST API)
        [HttpGet]
        public ActionResult CheckPaymentStatus(int orderId = 0)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            string userId = GetCurrentUserId();
            var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
            int? lastCreatedId = Session["LastCreatedOrderId"] != null ? (int?)Session["LastCreatedOrderId"] : null;

            var (isPaid, isExpired, paymentStatus, paymentMethod, message) =
                _checkoutService.CheckPaymentStatus(userId, orderId, pending, lastCreatedId);

            if (isExpired)
            {
                if (pending != null) Session.Remove("PendingCheckoutSession");
                return Json(new {
                    isPaid = false,
                    isExpired = true,
                    message = message,
                    orderId = orderId
                }, JsonRequestBehavior.AllowGet);
            }

            if (isPaid)
            {
                int targetOrderId = orderId > 0 ? orderId : (lastCreatedId ?? 0);
                if (pending != null && targetOrderId == 0)
                {
                    return ConfirmBankPaymentApi(0, 1);
                }

                return Json(new {
                    isPaid = true,
                    paymentStatus = 1,
                    paymentMethod = paymentMethod,
                    orderId = targetOrderId,
                    redirectUrl = Url.Action("Success", "Order", new { orderId = targetOrderId, paymentMethod = paymentMethod }),
                    message = message
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new {
                isPaid = false,
                paymentStatus = paymentStatus,
                paymentMethod = paymentMethod,
                orderId = orderId
            }, JsonRequestBehavior.AllowGet);
        }

        // GET: /Order/Track (Trang Tra cứu đơn hàng bằng SĐT hoặc Mã đơn)
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Track(string query = "")
        {
            ViewBag.InitialQuery = query;
            return View();
        }

        // POST: /Order/SearchOrderJson (AJAX tra cứu đơn hàng)
        [AllowAnonymous]
        [HttpPost]
        public ActionResult SearchOrderJson(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Vui lòng nhập Số điện thoại nhận hàng hoặc Mã đơn hàng!" });
            }

            string cleanQuery = query.Trim().Replace("#", "");
            var qOrders = DbContext.Order.AsQueryable();

            int parsedOrderId = 0;
            bool isNumericId = int.TryParse(cleanQuery, out parsedOrderId);

            if (isNumericId && parsedOrderId > 0)
            {
                qOrders = qOrders.Where(o => o.Id == parsedOrderId || (o.ReceiverPhone != null && o.ReceiverPhone.Contains(cleanQuery)));
            }
            else
            {
                qOrders = qOrders.Where(o => o.ReceiverPhone != null && o.ReceiverPhone.Contains(cleanQuery));
            }

            var orders = qOrders
                .Include("OrderDetails.Product")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (!orders.Any())
            {
                return Json(new { success = false, message = $"Không tìm thấy đơn hàng nào phù hợp với từ khóa '{query}'!" });
            }

            var orderList = orders.Select(o => new
            {
                id = o.Id,
                orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                rawOrderDate = o.OrderDate,
                receiverName = o.ReceiverName,
                receiverPhone = o.ReceiverPhone,
                shippingAddress = o.ShippingAddress,
                totalAmount = o.TotalAmount,
                discountAmount = o.DiscountAmount,
                shippingFee = o.ShippingFee,
                orderStatus = o.OrderStatus,
                orderStatusText = o.OrderStatus == 0 ? "Chờ duyệt" :
                                 o.OrderStatus == 1 ? "Đã duyệt" :
                                 o.OrderStatus == 2 ? "Đã đóng gói" :
                                 o.OrderStatus == 3 ? "Đang giao hàng" :
                                 o.OrderStatus == 4 ? "Giao thành công" : "Đã hủy",
                paymentMethod = o.PaymentMethod == 0 ? "COD (Tiền mặt)" : "Chuyển khoản VietinBank",
                paymentStatus = o.PaymentStatus == 1 ? "Đã thanh toán" : "Chưa thanh toán",
                rawPaymentStatus = o.PaymentStatus,
                rawOrderStatus = o.OrderStatus,
                isExpired = o.PaymentStatus == 0 && o.PaymentMethod == 1 && (DateTime.Now - o.OrderDate).TotalMinutes >= 10,
                paymentUrl = Url.Action("Payment", "Order", new { orderId = o.Id }),
                items = o.OrderDetails != null ? o.OrderDetails.Select(d => new
                {
                    productId = d.ProductId,
                    productName = d.Product != null ? d.Product.Name : "Sản phẩm #" + d.ProductId,
                    productImage = d.Product != null && !string.IsNullOrEmpty(d.Product.URLImage) ? d.Product.URLImage : "/Content/images/no-image.png",
                    quantity = d.Quantity,
                    price = d.Price,
                    total = d.Price * d.Quantity
                }).ToList() : null
            }).ToList();

            return Json(new { success = true, count = orderList.Count, orders = orderList });
        }

        // POST: /Order/CancelOrder (Hủy đơn hàng chưa nhận / chưa thanh toán)
        [AllowAnonymous]
        [HttpPost]
        public ActionResult CancelOrder(int orderId)
        {
            try
            {
                var order = DbContext.Order.Include("OrderDetails.Product").FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                if (order.OrderStatus == 3 || order.OrderStatus == 4 || order.OrderStatus == 5)
                {
                    return Json(new { success = false, message = "Đơn hàng đang giao, đã giao thành công hoặc đã hủy trước đó, không thể hủy!" });
                }

                if (order.PaymentStatus == 1)
                {
                    return Json(new { success = false, message = "Đơn hàng đã được thanh toán thành công, vui lòng liên hệ bộ phận hỗ trợ để hoàn tiền!" });
                }

                order.OrderStatus = 5;

                if (order.OrderDetails != null)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.Quantity += detail.Quantity;
                        }
                    }
                }

                DbContext.SaveChanges();

                return Json(new { success = true, message = $"Đã hủy thành công đơn hàng #{orderId}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi hủy đơn hàng: " + ex.Message });
            }
        }
    }
}
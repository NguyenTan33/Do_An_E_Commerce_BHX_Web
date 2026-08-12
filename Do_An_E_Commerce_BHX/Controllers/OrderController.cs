using System;
using System.Collections;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Controllers
{
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

            if (cart == null) return RedirectToAction("Index", "Cart");

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
            try
            {
                string userId = GetCurrentUserId();
                var (success, message, _, _, pendingSession) = _checkoutService.CreatePendingCheckoutSession(
                    userId, receiverName, receiverPhone, shippingAddress, paymentMethod, shippingFee, discountAmount, usedPoints, note, selectedIds, couponCode);

                if (!success) return Json(new { success = false, message });

                Session["PendingCheckoutSession"] = pendingSession;
                return Json(new
                {
                    success = true,
                    message,
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

        // GET: /Order/Payment (Trang quét mã VietQR)
        [HttpGet]
        public ActionResult Payment(int orderId = 0, int isPending = 0)
        {
            if (orderId > 0)
            {
                var order = DbContext.Order.Include("OrderDetails.Product").FirstOrDefault(o => o.Id == orderId);
                if (order != null) return View(order);
            }

            var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
            if (pending != null)
            {
                string userId = GetCurrentUserId();
                var pendingOrderModel = _checkoutService.GetPendingOrderForPaymentView(pending, userId);
                if (pendingOrderModel != null) return View(pendingOrderModel);
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
                    return Json(new
                    {
                        success = true,
                        message,
                        redirectUrl = Url.Action("Success", "Order", new { orderId = createdOrderId, paymentMethod = 0 })
                    });
                }
                return Json(new { success = false, message });
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

                    return Json(new
                    {
                        isPaid = true,
                        success = true,
                        message,
                        orderId = createdOrderId,
                        redirectUrl = Url.Action("Success", "Order", new { orderId = createdOrderId, paymentMethod })
                    }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, message }, JsonRequestBehavior.AllowGet);
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
                ViewBag.Order = DbContext.Order.Include("OrderDetails.Product").FirstOrDefault(o => o.Id == orderId);
            }

            return View();
        }

        // GET: /Order/CheckPaymentStatus (Auto-Polling SePay REST API)
        [HttpGet]
        public ActionResult CheckPaymentStatus(int orderId = 0)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            string userId = GetCurrentUserId();
            var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
            int? lastCreatedId = Session["LastCreatedOrderId"] as int?;

            var (isPaid, isExpired, paymentStatus, paymentMethod, message) =
                _checkoutService.CheckPaymentStatus(userId, orderId, pending, lastCreatedId);

            if (isExpired)
            {
                if (pending != null) Session.Remove("PendingCheckoutSession");
                return Json(new { isPaid = false, isExpired = true, message, orderId }, JsonRequestBehavior.AllowGet);
            }

            if (isPaid)
            {
                int targetOrderId = orderId > 0 ? orderId : (lastCreatedId ?? 0);
                if (pending != null && targetOrderId == 0)
                {
                    return ConfirmBankPaymentApi(0, 1);
                }

                return Json(new
                {
                    isPaid = true,
                    paymentStatus = 1,
                    paymentMethod,
                    orderId = targetOrderId,
                    redirectUrl = Url.Action("Success", "Order", new { orderId = targetOrderId, paymentMethod }),
                    message
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { isPaid = false, paymentStatus, paymentMethod, orderId }, JsonRequestBehavior.AllowGet);
        }

        // GET: /Order/Track (Trang Tra cứu đơn hàng)
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
            var (success, message, ordersData) = _checkoutService.SearchOrdersForTracking(query);
            if (!success)
            {
                return Json(new { success = false, message });
            }

            int count = 0;
            var ordersList = ordersData as IEnumerable;
            if (ordersList != null)
            {
                foreach (var item in ordersList) count++;
            }

            return Json(new { success = true, count, orders = ordersData });
        }

        // POST: /Order/CancelOrder (Hủy đơn hàng)
        [AllowAnonymous]
        [HttpPost]
        public ActionResult CancelOrder(int orderId)
        {
            var (success, message) = _checkoutService.CancelOrder(orderId);
            return Json(new { success, message });
        }
    }
}
using System;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Models; // Thêm namespace chứa các Service của ông

namespace Do_An_E_Commerce_BHX.Controllers
{
   
    public class OrderController : BaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        public OrderController()
        {
            _dbContext = new ApplicationDbContext();
            _cartService = new CartService(_dbContext);
            _orderService = new OrderService(_dbContext, new Calculate(), _cartService);
        }

        // GET: /Order/Index (Trang Thanh Toán)
        [HttpGet]
        public ActionResult Index(string selectedIds = null, string coupon = null)
        {
            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Nếu truyền selectedIds thì lọc danh sách các món chọn
            var selectedProductIds = new System.Collections.Generic.List<int>();
            if (!string.IsNullOrEmpty(selectedIds))
            {
                selectedProductIds = selectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(int.Parse)
                                                .ToList();
            }

            if (selectedProductIds.Any())
            {
                cart.CartDetails = cart.CartDetails.Where(cd => selectedProductIds.Contains(cd.ProductId)).ToList();
            }

            // Lấy thông tin User đã đăng nhập & Danh sách địa chỉ đã lưu
            bool isAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAuthenticated = isAuthenticated;

            if (isAuthenticated)
            {
                var userAddresses = _dbContext.UserAddresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ToList();
                ViewBag.UserAddresses = userAddresses;

                var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    ViewBag.UserFullName = user.FullName;
                    ViewBag.UserPhone = user.PhoneNumber;
                    ViewBag.LoyaltyPoints = user.LoyaltyPoints;
                }
                else
                {
                    ViewBag.LoyaltyPoints = 0;
                }
            }
            else
            {
                ViewBag.LoyaltyPoints = 0;
            }

            // Xử lý mã giảm giá Coupon qua VoucherService Shopee-style
            var voucherService = new VoucherService(_dbContext);
            var cartItems = cart != null && cart.CartDetails != null ? cart.CartDetails.ToList() : new System.Collections.Generic.List<CartDetail>();
            var suggestedVouchers = voucherService.GetSuggestedVouchersForCart(cartItems, userId);
            ViewBag.SuggestedVouchers = suggestedVouchers;

            decimal discountAmount = 0;
            string appliedCode = "";
            string couponMessage = "";

            if (!string.IsNullOrWhiteSpace(coupon))
            {
                var codeUpper = coupon.Trim().ToUpper();
                var promo = _dbContext.Promotion.Include("Category").FirstOrDefault(p => p.Code.ToUpper() == codeUpper);
                if (promo != null)
                {
                    var eval = voucherService.EvaluateVoucher(promo, cartItems, userId);
                    if (eval.IsEligible)
                    {
                        discountAmount = (decimal)eval.CalculatedDiscount;
                        appliedCode = promo.Code;
                    }
                    else
                    {
                        couponMessage = eval.ReasonIfNotEligible;
                    }
                }
                else
                {
                    couponMessage = $"Mã giảm giá '{coupon}' không tồn tại trên hệ thống!";
                }
            }

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
                var selectedProductIds = new System.Collections.Generic.List<int>();
                if (!string.IsNullOrEmpty(selectedIds))
                {
                    selectedProductIds = selectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(int.Parse)
                                                    .ToList();
                }

                Promotion couponObj = null;
                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    var codeUpper = couponCode.Trim().ToUpper();
                    couponObj = _dbContext.Promotion.FirstOrDefault(p => p.Code.ToUpper() == codeUpper && p.IsActive);
                }

                var createdOrder = _orderService.CreateOrder(
                    userId: userId,
                    receiverName: receiverName,
                    receiverPhone: receiverPhone,
                    shippingAddress: shippingAddress,
                    coupon: couponObj,
                    paymentMethod: paymentMethod,
                    shippingFee: shippingFee,
                    discountAmount: discountAmount,
                    usedPoints: usedPoints,
                    note: note,
                    selectedProductIds: selectedProductIds
                );

                return Json(new { 
                    success = true, 
                    message = "Tạo đơn hàng thành công!", 
                    orderId = createdOrder != null ? createdOrder.Id : 0, 
                    redirectUrl = Url.Action("Payment", "Order", new { orderId = createdOrder != null ? createdOrder.Id : 0 })
                });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                var fullErrorMessage = string.Join(" | ", errorMessages);

                return Json(new { success = false, message = "LỖI TẠI CỘT: " + fullErrorMessage });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var current = ex.InnerException;
                while (current != null)
                {
                    msg += " -> " + current.Message;
                    current = current.InnerException;
                }
                return Json(new { success = false, message = "LỖI ĐẶT HÀNG: " + msg });
            }
        }

        // GET: /Order/Payment?orderId=123 (Trang chọn & hoàn tất thanh toán chuyên biệt)
        [HttpGet]
        public ActionResult Payment(int orderId)
        {
            var order = _dbContext.Order
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        private string GetLoggedUserEmail(Order order)
        {
            if (order != null && order.User != null)
            {
                if (!string.IsNullOrWhiteSpace(order.User.Email) && order.User.Email.Contains("@")) return order.User.Email;
                if (!string.IsNullOrWhiteSpace(order.User.UserName) && order.User.UserName.Contains("@")) return order.User.UserName;
            }

            if (User.Identity.IsAuthenticated)
            {
                string currentName = User.Identity.Name;
                if (!string.IsNullOrWhiteSpace(currentName) && currentName.Contains("@")) return currentName;

                string currentId = User.Identity.GetUserId();
                if (!string.IsNullOrWhiteSpace(currentId))
                {
                    var u = _dbContext.Users.FirstOrDefault(x => x.Id == currentId);
                    if (u != null)
                    {
                        if (!string.IsNullOrWhiteSpace(u.Email) && u.Email.Contains("@")) return u.Email;
                        if (!string.IsNullOrWhiteSpace(u.UserName) && u.UserName.Contains("@")) return u.UserName;
                    }
                }
            }

            return null;
        }

        // POST: /Order/ProcessCOD (Xử lý hoàn tất đặt hàng COD)
        [HttpPost]
        public ActionResult ProcessCOD(int orderId)
        {
            try
            {
                var order = _dbContext.Order.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                order.PaymentMethod = 0; // COD
                order.PaymentStatus = 0; // Chưa thanh toán (chờ giao hàng thu tiền)
                order.OrderStatus = 0;   // Chờ duyệt
                _dbContext.SaveChanges();

                // Lấy thông tin order đầy đủ để dựng Email Hóa đơn
                var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                string userEmail = GetLoggedUserEmail(fullOrder ?? order);
                string msgText = "Đặt hàng thành công với phương thức COD!";

                // Chỉ gửi Email Hóa đơn khi người dùng ĐÃ ĐĂNG NHẬP và có email hợp lệ
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? order, userEmail);
                    msgText = "Đặt hàng thành công! Hóa đơn điện tử đã được phát hành và gửi tới email " + userEmail;
                }

                return Json(new { 
                    success = true, 
                    message = msgText, 
                    redirectUrl = Url.Action("Index", "Home") 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xử lý COD: " + ex.Message });
            }
        }

        // POST: /Order/ConfirmBankPaymentApi (API mô phỏng / xác nhận thanh toán chuyển khoản / MoMo thành công)
        [HttpPost]
        public ActionResult ConfirmBankPaymentApi(int orderId, int paymentMethod = 1)
        {
            try
            {
                var order = _dbContext.Order.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                order.PaymentMethod = paymentMethod; // 1 = VietQR / Ngân hàng, 2 = MoMo
                order.PaymentStatus = 1; // 1 = Đã thanh toán thành công
                order.OrderStatus = 0;   // Chờ duyệt / chuẩn bị hàng
                _dbContext.SaveChanges();

                // Lấy thông tin order đầy đủ để dựng Email Hóa đơn
                var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                string userEmail = GetLoggedUserEmail(fullOrder ?? order);
                string methodText = paymentMethod == 2 ? "Ví MoMo" : "Chuyển khoản Ngân hàng";
                string msgText = $"Thanh toán qua {methodText} thành công!";

                // Chỉ gửi Email Hóa đơn khi người dùng ĐÃ ĐĂNG NHẬP và có email hợp lệ
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? order, userEmail);
                    msgText = $"Thanh toán qua {methodText} thành công! Hóa đơn điện tử đã được phát hành và gửi tới email " + userEmail;
                }

                return Json(new { 
                    success = true, 
                    message = msgText, 
                    redirectUrl = Url.Action("Index", "Home") 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xác nhận thanh toán: " + ex.Message });
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
                var order = _dbContext.Order
                    .Include("OrderDetails.Product")
                    .FirstOrDefault(o => o.Id == orderId);
                ViewBag.Order = order;
            }

            return View();
        }

        // GET: /Order/CheckPaymentStatus?orderId=123 (Phục vụ Auto-Polling kiểm tra tiền về từ SePay VietinBank)
        [HttpGet]
        public ActionResult CheckPaymentStatus(int orderId)
        {
            var order = _dbContext.Order.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return Json(new { isPaid = false, message = "Không tìm thấy đơn" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { 
                isPaid = order.PaymentStatus == 1, 
                paymentStatus = order.PaymentStatus, 
                paymentMethod = order.PaymentMethod,
                orderId = orderId
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}
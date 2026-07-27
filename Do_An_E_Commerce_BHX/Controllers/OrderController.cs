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

            // Xử lý mã giảm giá Coupon nếu có
            decimal discountAmount = 0;
            string appliedCode = "";
            if (!string.IsNullOrWhiteSpace(coupon))
            {
                var codeUpper = coupon.Trim().ToUpper();
                var now = DateTime.Now;
                var promo = _dbContext.Promotion.FirstOrDefault(p => p.Code.ToUpper() == codeUpper && p.IsActive);
                if (promo != null && promo.EffectiveDate <= now && promo.ExpiryDate >= now)
                {
                    decimal subTotal = cart.CartDetails.Sum(cd => (cd.Product != null ? cd.Product.Price : 0) * cd.Quantity);
                    if (promo.percentDiscount > 0)
                    {
                        decimal rate = promo.percentDiscount;
                        if (rate > 1) rate = rate / 100m; // Ví dụ: 90% -> 0.90
                        discountAmount = subTotal * rate;
                    }
                    else if (promo.DiscountValue > 0)
                    {
                        discountAmount = promo.DiscountValue;
                    }

                    if (discountAmount > subTotal) discountAmount = subTotal;
                    appliedCode = promo.Code;
                }
            }

            ViewBag.CouponCode = appliedCode;
            ViewBag.DiscountAmount = discountAmount;
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

                return Json(new { 
                    success = true, 
                    message = "Đặt hàng thành công với phương thức COD!", 
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

                string methodText = paymentMethod == 2 ? "Ví MoMo" : "Chuyển khoản Ngân hàng";

                return Json(new { 
                    success = true, 
                    message = $"Thanh toán qua {methodText} thành công!", 
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}
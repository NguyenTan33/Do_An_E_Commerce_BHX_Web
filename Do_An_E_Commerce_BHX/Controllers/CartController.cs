using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Controllers
{
    [AllowAnonymous]
    public class CartController : BaseController
    {
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        public CartController()
        {
            _cartService = new CartService(DbContext);
            _orderService = new OrderService(DbContext, new Calculate(), _cartService);
        }

        public CartController(ApplicationDbContext dbContext) : base(dbContext)
        {
            _cartService = new CartService(DbContext);
            _orderService = new OrderService(DbContext, new Calculate(), _cartService);
        }

        // 1. Render Trang Giỏ Hàng
        public ActionResult Index(string coupon = null)
        {
            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            ViewBag.TotalPrice = _orderService.CalculatePrice(userId);

            var voucherService = new VoucherService(DbContext);
            var cartItems = cart != null && cart.CartDetails != null ? cart.CartDetails.ToList() : new List<CartDetail>();
            var suggestedVouchers = voucherService.GetSuggestedVouchersForCart(cartItems, userId);

            ViewBag.SuggestedVouchers = suggestedVouchers;
            ViewBag.AppliedCoupon = coupon;

            return View(cart);
        }

        // 2. Thêm vào giỏ (Gọi qua AJAX)
        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity = 1)
        {
            try
            {
                string userId = GetCurrentUserId();
                _cartService.AddItemToCart(productId, userId, quantity);
                decimal newTotal = _orderService.CalculatePrice(userId);
                return Json(new { success = true, newTotal = newTotal });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var msg = "";
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        msg += string.Format("[{0}: {1}] ", validationError.PropertyName, validationError.ErrorMessage);
                    }
                }
                return Json(new { success = false, message = "Lỗi dữ liệu giỏ hàng: " + msg });
            }
            catch (System.Exception ex)
            {
                var msg = ex.Message;
                var current = ex.InnerException;
                while (current != null)
                {
                    msg += " -> " + current.Message;
                    current = current.InnerException;
                }
                return Json(new { success = false, message = "Lỗi khi thêm vào giỏ: " + msg });
            }
        }

        // 3. Đổi số lượng (Gọi qua AJAX)
        [HttpPost]
        public JsonResult ChangeQuantity(int productId, int amount)
        {
            string userId = GetCurrentUserId();
            int finalAmount = _cartService.ChangeQuantity(userId, productId, amount);
            if (finalAmount == 0)
            {
                return Json(new { success = false , massage = "số lượng vượt tồn kho hoặc ko hợp lệ" });
            }
            decimal newTotal = _orderService.CalculatePrice(userId);
            return Json(new { success = true, newTotal = newTotal , finalAmount  = finalAmount });
        }

        // 4. Xóa khỏi giỏ (Gọi qua AJAX)
        [HttpPost]
        public JsonResult RemoveItem(int productId)
        {
            string userId = GetCurrentUserId();
            _cartService.RemoveItemFromCart(productId, userId);
            decimal newTotal = _orderService.CalculatePrice(userId);
            return Json(new { success = true, newTotal = newTotal });
        }

        // 5. Xóa các mục đã chọn (Gọi qua AJAX)
        [HttpPost]
        public JsonResult RemoveSelected(List<int> productIds)
        {
            string userId = GetCurrentUserId();
            _cartService.RemoveSelectedItemsFromCart(productIds, userId);

            decimal newTotal = _orderService.CalculatePrice(userId);
            return Json(new { success = true, newTotal = newTotal });
        }

        // 6. Xóa tất cả sản phẩm trong giỏ (Gọi qua AJAX)
        [HttpPost]
        public JsonResult ClearAll()
        {
            string userId = GetCurrentUserId();
            _cartService.ClearCart(userId);

            return Json(new { success = true, newTotal = 0 });
        }

        // 7. Áp dụng mã giảm giá (Coupon)
        [HttpPost]
        public JsonResult ApplyCoupon(string couponCode, decimal subTotal)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá!" });
            }

            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);
            var cartItems = cart != null && cart.CartDetails != null ? cart.CartDetails.ToList() : new List<CartDetail>();

            var codeUpper = couponCode.Trim().ToUpper();
            var promotion = DbContext.Promotion.Include("Category").FirstOrDefault(p => p.Code.ToUpper() == codeUpper);

            if (promotion == null)
            {
                return Json(new { success = false, message = $"Mã giảm giá '{couponCode}' không tồn tại trên hệ thống!" });
            }

            var voucherService = new VoucherService(DbContext);
            var eval = voucherService.EvaluateVoucher(promotion, cartItems, userId);

            if (!eval.IsEligible)
            {
                return Json(new { success = false, message = eval.ReasonIfNotEligible });
            }

            return Json(new
            {
                success = true,
                message = $"🎉 Áp dụng thành công mã {promotion.Code}! Được giảm {eval.CalculatedDiscount:N0} VNĐ.",
                code = promotion.Code,
                discountAmount = (decimal)eval.CalculatedDiscount
            });
        }

        // 8. Header Cart Summary Child Action
        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            int totalQuantity = 0;
            decimal cartTotal = 0;

            if (cart != null && cart.CartDetails != null)
            {
                totalQuantity = cart.CartDetails.Sum(i => i.Quantity);
                cartTotal = cart.CartDetails.Sum(i => (decimal)(i.Product != null ? i.Product.Price : 0) * i.Quantity);
            }

            ViewBag.CartCount = totalQuantity;
            ViewBag.CartTotal = cartTotal;
            return PartialView("_CartSummary", cart);
        }
    }
}
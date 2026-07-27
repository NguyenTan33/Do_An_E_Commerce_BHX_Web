using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Models;
using System.Linq;
namespace Do_An_E_Commerce_BHX.Controllers
{using System.Linq;
    [AllowAnonymous]
    public class CartController : BaseController
    {
        private readonly ApplicationDbContext _dbContext; // 1. Khai báo biến _dbContext ở đây
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        // DI hoặc khởi tạo trong Constructor
        public CartController()
        {
            // 2. Gán vào biến _dbContext của Class
            _dbContext = new ApplicationDbContext();
            _cartService = new CartService(_dbContext);
            _orderService = new OrderService(_dbContext, new Calculate(), _cartService);
        }

        // 1. Render Trang Giỏ Hàng
        public ActionResult Index(string coupon = null)
        {
            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            // Lấy tổng tiền chưa discount
            ViewBag.TotalPrice = _orderService.CalculatePrice(userId);

            // Gợi ý danh sách mã giảm giá khả dụng theo Shopee
            var voucherService = new VoucherService(_dbContext);
            var cartItems = cart != null && cart.CartDetails != null ? cart.CartDetails.ToList() : new System.Collections.Generic.List<Do_An_E_Commerce_BHX.Models.Entities.CartDetail>();
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
        public JsonResult RemoveSelected(System.Collections.Generic.List<int> productIds)
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
            var cartItems = cart != null && cart.CartDetails != null ? cart.CartDetails.ToList() : new System.Collections.Generic.List<Do_An_E_Commerce_BHX.Models.Entities.CartDetail>();

            var codeUpper = couponCode.Trim().ToUpper();
            var promotion = _dbContext.Promotion.Include("Category").FirstOrDefault(p => p.Code.ToUpper() == codeUpper);

            if (promotion == null)
            {
                return Json(new { success = false, message = $"Mã giảm giá '{couponCode}' không tồn tại trên hệ thống!" });
            }

            var voucherService = new VoucherService(_dbContext);
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
                cartTotal = cart.CartDetails.Sum(i => (i.Product != null ? i.Product.Price : 0) * i.Quantity);
            }

            ViewBag.CartCount = totalQuantity;
            ViewBag.CartTotal = cartTotal;
            return PartialView("_CartSummary", cart);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}
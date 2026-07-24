using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models; // Add thêm dòng này để gọi ApplicationDbContext
namespace Do_An_E_Commerce_BHX.Controllers
{
    [AllowAnonymous]
    public class CartController : BaseController 
    {
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        // DI hoặc khởi tạo trong Constructor
        public CartController()
        {
            // Tự khởi tạo mấy cái dependency bằng tay
            var dbContext = new ApplicationDbContext();
            _cartService = new CartService(dbContext);
            _orderService = new OrderService(dbContext, new Calculate(), _cartService);
        }

        // 1. Render Trang Giỏ Hàng
        public ActionResult Index()
        {
            string userId =GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            // Lấy tổng tiền chưa discount
            ViewBag.TotalPrice = _orderService.CalculatePrice(userId);

            return View(cart);
        }

        // 2. Thêm vào giỏ (Gọi qua AJAX)
        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity = 1)
        {
            string userId = GetCurrentUserId();
            _cartService.AddItemToCart(productId, userId, quantity);
            decimal newTotal = _orderService.CalculatePrice(userId);
            return Json(new { success = true, newTotal = newTotal });
        }

        // 3. Đổi số lượng (Gọi qua AJAX)
        [HttpPost]
        public JsonResult ChangeQuantity(int productId, int amount)
        {
            string userId = GetCurrentUserId();
            _cartService.ChangeQuantity(userId, productId, amount);

            decimal newTotal = _orderService.CalculatePrice(userId);
            return Json(new { success = true, newTotal = newTotal });
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
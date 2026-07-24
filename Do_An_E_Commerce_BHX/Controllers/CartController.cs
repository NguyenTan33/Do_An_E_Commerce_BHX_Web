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
        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            // Kiểm tra null và lấy danh sách item theo đúng property trong Model Cart của ông
            // Nếu trong Cart Model của ông tên là Items hoặc Carts thì đổi tên tương ứng
            int totalQuantity = 0;

            if (cart != null)
            {
                // Hoặc cart.Items.Sum(...) tùy tên property trong model Cart
                totalQuantity = cart.CartDetails?.Sum(i => i.Quantity) ?? 0;
            }

            ViewBag.CartCount = totalQuantity;
            return PartialView("_CartSummary", cart);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}
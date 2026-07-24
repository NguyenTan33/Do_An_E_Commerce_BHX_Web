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
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        // Nếu ông dùng DI (Dependency Injection) thì inject vào, 
        // còn nếu new tay thì khởi tạo trong Constructor như bên dưới:
        public OrderController()
        {
            var dbContext = new ApplicationDbContext(); // Đổi lại đúng tên DbContext của ông
            _cartService = new CartService(dbContext);
            _orderService = new OrderService(dbContext, new Calculate(), _cartService);
        }

        // GET: /Order/Index (Trang Thanh Toán)
        [HttpGet]
        public ActionResult Index()
        {
            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            // Giỏ hàng trống thì đẩy về trang Giỏ hàng
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cart); // Truyền Cart sang View để hiển thị lại các món hàng
        }

        // POST: /Order/Checkout (Xử lý chốt đơn qua AJAX)
        [HttpPost]
        public ActionResult Checkout(string receiverName, string receiverPhone, string shippingAddress)
        {
            string userId = GetCurrentUserId();

            try
            {
                _orderService.CreateOrder(userId, receiverName, receiverPhone, shippingAddress);
                return Json(new { success = true, message = "Đặt hàng thành công!" });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                // 🚨 Lấy chính xác TÊN CỘT bị thiếu làm Order không lưu được
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                var fullErrorMessage = string.Join(" | ", errorMessages);

                return Json(new { success = false, message = "LỖI TẠI CỘT: " + fullErrorMessage });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "LỖI SQL: " + inner });
            }
        }

        // GET: /Order/Success (Trang thông báo hoàn tất)
        [HttpGet]
        public ActionResult Success()
        {
            return View();
        }
    }
}
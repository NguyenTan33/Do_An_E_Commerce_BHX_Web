using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class OrderService
    {
        public ApplicationDbContext dbContext;
        public Calculate calculate;
        public CartService cartService;
        public OrderService(ApplicationDbContext appDBContext, Calculate calculate, CartService cartService)
        {
            this.dbContext = appDBContext;
            this.calculate = calculate;
            this.cartService = cartService;
        }

        //♥hàm tạo đơn thanh toán(tạo Order Chép từng product trong CartDetail qua OrderDetail , trừ kho và tạo Order nhét vào db

        public void CreateOrder(string userId, string receiverName, string receiverPhone, string shippingAddress, Promotion coupon = null)
        {
            // 1. Lấy Cart
            var cart = cartService.GetCartByUserId(userId);
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                throw new Exception("Giỏ hàng trống hoặc mất phiên (Session/Cookie) của khách vãng lai!");

            bool isRealUser = dbContext.Users.Any(u => u.Id == userId);

            // 2. Tính tiền (dùng calculate)
            decimal rawTotal = calculate.CalculatePrice(cart.CartDetails.ToList());
            decimal finalTotal = calculate.applyCoupon(rawTotal, coupon);

            // 3. Tạo Order
            var order = new Order
            {
                UserId = isRealUser ? userId : null, // User thật thì gán Id, Guest thì để NULL
                OrderDate = DateTime.Now,
                TotalAmount = finalTotal,
                OrderStatus = 0,
                PaymentMethod = 0,
                PaymentStatus = 0,

                ReceiverName = receiverName,
                ReceiverPhone = receiverPhone,
                ShippingAddress = shippingAddress,

                OrderDetails = new List<OrderDetail>()
            };

            // 4. Map CartDetails sang OrderDetails + Trừ tồn kho
            foreach (var item in cart.CartDetails.ToList())
            {
                // Query trực tiếp từ DB để né lỗi NullReferenceException do item.Product bị null
                var product = dbContext.Product.Find(item.ProductId);
                if (product != null)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price
                    });

                    // Trừ tồn kho
                    product.Quantity -= item.Quantity;
                }
            }

            // Add Order vào DbContext
            dbContext.Order.Add(order);

            // 5. Xóa chi tiết giỏ hàng
            var cartDetailsToRemove = cart.CartDetails.ToList();
            dbContext.CartDetail.RemoveRange(cartDetailsToRemove);

            // 6. Thực thi lưu xuống SQL Server
            dbContext.SaveChanges();
        }

        // Dành cho User đã đăng nhập
        public List<Order> GetOrdersByUserId(string userId)
        {
            return dbContext.Order
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }

        // Dành cho Khách vãng lai (Tra cứu bằng SĐT)
        public List<Order> GetOrdersByPhone(string phone)
        {
            return dbContext.Order
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .Where(o => o.ReceiverPhone == phone)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }

        public void CancelOrder(string userID ,  int orderID)
        {
            var order = dbContext.Order
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .FirstOrDefault(o => o.Id == orderID);
            if (order == null) return;
            if (order.OrderStatus != 0) return;

            order.OrderStatus = 4;

            // HOÀN TRẢ SỐ LƯỢNG VÀO KHO
            foreach (var detail in order.OrderDetails)
            {
                var product = dbContext.Product.Find(detail.ProductId);
                if (product != null)
                {
                    product.Quantity += detail.Quantity; 
                }
            }
            dbContext.SaveChanges();
        }

        //♥cái này là hàm tính tiền chưa Discount nha Tân - nếu null cart return 0
        public decimal CalculatePrice(string userId)
        {
            var cart = cartService.GetCartByUserId(userId);

            if (cart == null) return 0;

            var cartDetailList = cart.CartDetails.ToList();
            decimal totalValue = calculate.CalculatePrice(cartDetailList);
            return totalValue;
        }
        //cái này là hàm tính tiền đã qua Discount nha Tân
        public decimal CalculatePriceAfterApplyCoupon(decimal money , Promotion coupon)
        {
            decimal totalValueAfterApplyCoupon = calculate.applyCoupon(money, coupon);
            return totalValueAfterApplyCoupon;
        }
    }

    public class Calculate
    {
        public decimal CalculatePrice(List<CartDetail> cartDetails)
        {
            decimal totalValue = 0;
            foreach (CartDetail item in cartDetails)
            {
                totalValue += item.Product.Price * item.Quantity;
            }
            return totalValue;
        }
        public decimal applyCoupon(decimal money, Promotion coupon)
        {
            if (coupon != null)
            {
                var percent = coupon.percentDiscount;
                var y = DiscountForTotal(money, 0, percent);
                return y;
            }
            var x = DiscountForTotal(money, 0, 0);
            return x;
        }
        private decimal DiscountForTotal(decimal baseValue, decimal percent, decimal totalPercent)
        {
            decimal finalPrice = baseValue * (1 - percent) * (1 - totalPercent);

            return finalPrice;
        }
    }
    //public class Coupon
    //{
    //    public int id;
    //    public string name;
    //    public string description;
    //    public decimal percentDiscount;
    //}
}
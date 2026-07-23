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

        //♥hàm tạo đơn thanh toán
        public void CreateOrder(string userId, Promotion coupon = null)
        {
            // 1. Lấy Cart
            var cart =cartService.GetCartByUserId(userId);
            if (cart == null || !cart.CartDetails.Any()) return;

            // 2. Tính tiền gốc & tiền sau giảm
            decimal rawTotal = calculate.CalculatePrice(cart.CartDetails.ToList());
            decimal finalTotal = calculate.applyCoupon(rawTotal, coupon);

            // 3. Tạo Order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = finalTotal,
                OrderStatus = 0,
                OrderDetails = new List<OrderDetail>()
            };

            // 4. Map từ CartDetails sang OrderDetails (Chốt giá!)
            foreach (var item in cart.CartDetails)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price // ĐÓNG BĂNG GIÁ TẠI THỜI ĐIỂM MUA
                });
            }

            dbContext.Order.Add(order);

            // 5. Dọn sạch giỏ hàng sau khi đặt thành công
            dbContext.CartDetail.RemoveRange(cart.CartDetails);

            dbContext.SaveChanges();
        }


        //♥cái này là hàm tính tiền chưa Discount nha Tân - nếu null cart return 0
        public decimal CalculatePrice(string userId)
        {
            //var cart = dbContext.Cart.FirstOrDefault(c => c.UserId == userId);
            var cart = dbContext.Cart
                .Include(c => c.CartDetails)
                .Include("CartDetails.Product") 
                .FirstOrDefault(c => c.UserId == userId);

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
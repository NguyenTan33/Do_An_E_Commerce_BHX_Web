using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class OrderService
    {
        public ApplicationDbContext dbContext;
        public Calculate calculate;
        public OrderService(ApplicationDbContext appDBContext, Calculate calculate)
        {
            this.dbContext = appDBContext;
            this.calculate = calculate;
        }
        public void CalculatePrice(int userId)
        {
            var cart = dbContext.Cart.FirstOrDefault(c => c.UserId == userId);
            if (cart == null) return;
            //var x = cart.CartDetails.ToList();
            //calculate.CalculatePrice(x);
        }
    }

    public class Calculate
    {
        public void CalculatePrice(List<Product> products)
        {
            decimal totalValue = 0;
            foreach (Product product in products)
            {
                totalValue += product.Price;
            }
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
        public decimal DiscountForTotal(decimal baseValue, decimal percent, decimal totalPercent)
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
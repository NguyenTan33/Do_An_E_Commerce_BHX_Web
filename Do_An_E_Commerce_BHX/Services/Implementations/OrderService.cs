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

        public Order CreateOrder(string userId, string receiverName, string receiverPhone, string shippingAddress, Promotion coupon = null, int paymentMethod = 0, decimal shippingFee = 0, decimal discountAmount = 0, int usedPoints = 0, double pointDiscountAmount = 0, string note = "", List<int> selectedProductIds = null)
        {
            // 1. Lấy Cart
            var cart = cartService.GetCartByUserId(userId);
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                throw new Exception("Giỏ hàng trống hoặc mất phiên (Session/Cookie) của khách vãng lai!");

            var itemsToOrder = cart.CartDetails.ToList();
            if (selectedProductIds != null && selectedProductIds.Any())
            {
                itemsToOrder = itemsToOrder.Where(cd => selectedProductIds.Contains(cd.ProductId)).ToList();
            }

            if (!itemsToOrder.Any())
                throw new Exception("Vui lòng chọn ít nhất một sản phẩm để đặt hàng!");

            var user = dbContext.Users.FirstOrDefault(u => u.Id == userId);
            bool isRealUser = (user != null);

            // 2. Kiểm tra & Khấu trừ Điểm tích lũy (Reward Points: 100 điểm = 1.000đ => 1 điểm = 10đ)
            if (isRealUser && usedPoints > 0)
            {
                if (user.LoyaltyPoints < usedPoints)
                {
                    usedPoints = user.LoyaltyPoints; // Tối đa số điểm đang có
                }
                pointDiscountAmount = (double)(usedPoints * 10);
                user.LoyaltyPoints -= usedPoints;
            }
            else
            {
                usedPoints = 0;
                pointDiscountAmount = 0;
            }

            // 3. Tính tiền
            decimal rawTotal = calculate.CalculatePrice(itemsToOrder);

            if (coupon != null && discountAmount == 0)
            {
                decimal totalAfterCoupon = calculate.applyCoupon(rawTotal, coupon);
                discountAmount = rawTotal - totalAfterCoupon;
            }

            decimal totalAfterDiscount = rawTotal - discountAmount - (decimal)pointDiscountAmount;
            if (totalAfterDiscount < 0) totalAfterDiscount = 0;

            decimal finalTotal = totalAfterDiscount + shippingFee;

            // 4. Tính điểm tích lũy thưởng cho đơn hàng này (+1 điểm cho mỗi 10.000đ)
            int earnedPoints = (int)(finalTotal / 10000m);
            if (isRealUser && earnedPoints > 0)
            {
                user.LoyaltyPoints += earnedPoints;
            }

            // 5. Tạo Order
            var order = new Order
            {
                UserId = isRealUser ? userId : null, // User thật thì gán Id, Guest thì để NULL
                OrderDate = DateTime.Now,
                TotalAmount = Convert.ToDouble(finalTotal),
                DiscountAmount = Convert.ToDouble(discountAmount),
                ShippingFee = Convert.ToDouble(shippingFee),
                OrderStatus = 0,
                PaymentMethod = paymentMethod, // 0 = COD, 1 = Bank Transfer / QR, 2 = MoMo
                PaymentStatus = 0,

                ReceiverName = receiverName,
                ReceiverPhone = receiverPhone,
                ShippingAddress = shippingAddress,
                UsedPoints = usedPoints,
                EarnedPoints = earnedPoints,
                PointDiscountAmount = pointDiscountAmount,
                Note = note,

                OrderDetails = new List<OrderDetail>()
            };

            // 6. Map CartDetails sang OrderDetails + Trừ tồn kho
            foreach (var item in itemsToOrder)
            {
                var product = dbContext.Product.Find(item.ProductId);
                if (product != null)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = Convert.ToDouble(product.Price)
                    });

                    // Trừ tồn kho
                    product.Quantity -= item.Quantity;
                }
            }

            // Add Order vào DbContext
            dbContext.Order.Add(order);

            // 7. Xóa chi tiết giỏ hàng tương ứng
            dbContext.CartDetail.RemoveRange(itemsToOrder);

            // 8. Thực thi lưu xuống SQL Server
            dbContext.SaveChanges();

            return order;
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
            if (coupon == null || money <= 0) return money;

            decimal discountAmount = 0;
            if (coupon.percentDiscount > 0)
            {
                decimal rate = coupon.percentDiscount;
                if (rate > 1) rate = rate / 100m; // Ví dụ: 90% -> 0.90
                discountAmount = money * rate;
            }
            else if (coupon.DiscountValue > 0)
            {
                discountAmount = coupon.DiscountValue;
            }

            if (discountAmount > money) discountAmount = money;
            return money - discountAmount;
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
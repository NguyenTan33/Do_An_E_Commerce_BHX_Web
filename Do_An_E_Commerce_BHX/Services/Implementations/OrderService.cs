using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Threading.Tasks;

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

            if (coupon != null)
            {
                if (discountAmount == 0)
                {
                    decimal totalAfterCoupon = calculate.applyCoupon(rawTotal, coupon);
                    discountAmount = rawTotal - totalAfterCoupon;
                }

                // Tăng số lượt đã dùng của Voucher và kiểm tra hết lượt để tự động tắt mã
                var dbCoupon = dbContext.Promotion.FirstOrDefault(p => p.Id == coupon.Id || p.Code == coupon.Code);
                if (dbCoupon != null)
                {
                    dbCoupon.UsedCount++;
                    if (dbCoupon.UsageLimit > 0 && dbCoupon.UsedCount >= dbCoupon.UsageLimit)
                    {
                        dbCoupon.IsActive = false; // Tự động ngắt mã không cho dùng nữa
                    }
                }
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

            // 6. Map CartDetails sang OrderDetails + Trừ tồn kho theo hệ số quy đổi (Kho gốc hoặc Sản phẩm hiện tại)
            foreach (var item in itemsToOrder)
            {
                var product = dbContext.Product.Find(item.ProductId);
                if (product != null)
                {
                    // Nếu sản phẩm này có Kho Gốc (ParentProductId), lấy thông tin kho từ sản phẩm gốc
                    var targetStockProduct = (product.ParentProductId.HasValue && product.ParentProductId.Value > 0)
                        ? dbContext.Product.Find(product.ParentProductId.Value) ?? product
                        : product;

                    int factor = product.UnitMultiplier > 0 ? product.UnitMultiplier : 1;
                    int requiredStock = item.Quantity * factor;

                    if (targetStockProduct.Quantity < requiredStock)
                    {
                        string unitLabel = !string.IsNullOrEmpty(product.Unit) ? product.Unit : "sản phẩm";
                        throw new Exception($"Sản phẩm '{product.Name}' hiện kho gốc chỉ còn {targetStockProduct.Quantity} lẻ, không đủ {requiredStock} lẻ để đóng {item.Quantity} {unitLabel}!");
                    }

                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = Convert.ToDouble(product.Price)
                    });

                    // Trừ tồn kho của sản phẩm gốc (hoặc sản phẩm hiện tại)
                    targetStockProduct.Quantity -= requiredStock;
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

            // HOÀN TRẢ SỐ LƯỢNG VÀO KHO (Trả về kho gốc nếu là bài quy cách con Thùng/Lốc)
            foreach (var detail in order.OrderDetails)
            {
                var product = dbContext.Product.Find(detail.ProductId);
                if (product != null)
                {
                    var targetStockProduct = (product.ParentProductId.HasValue && product.ParentProductId.Value > 0)
                        ? dbContext.Product.Find(product.ParentProductId.Value) ?? product
                        : product;

                    int factor = product.UnitMultiplier > 0 ? product.UnitMultiplier : 1;
                    int quantityToRestore = detail.Quantity * factor;

                    targetStockProduct.Quantity += quantityToRestore;
                }
            }
            dbContext.Order.Remove(order);
            dbContext.SaveChanges();
        }

        // ===== CÁC PHƯƠNG THỨC DÀNH CHO ADMIN MANAGE ORDER =====
        public async Task<List<Order>> GetAdminOrdersAsync(string search = "", int? status = 0)
        {
            var query = dbContext.Order
                .Include(o => o.User)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                int orderIdSearch;
                bool isNumeric = int.TryParse(s, out orderIdSearch);

                query = query.Where(o => o.ReceiverPhone.Contains(s) || 
                                         o.ReceiverName.Contains(s) || 
                                         (isNumeric && o.Id == orderIdSearch));
            }

            if (status.HasValue && status.Value >= 0)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetOrderCountsAsync()
        {
            var dict = new Dictionary<string, int>();
            dict["CountAll"] = await dbContext.Order.AsNoTracking().CountAsync();
            dict["CountPending"] = await dbContext.Order.AsNoTracking().CountAsync(o => o.OrderStatus == 0);
            dict["CountApproved"] = await dbContext.Order.AsNoTracking().CountAsync(o => o.OrderStatus == 1);
            dict["CountPacked"] = await dbContext.Order.AsNoTracking().CountAsync(o => o.OrderStatus == 2);
            dict["CountDelivering"] = await dbContext.Order.AsNoTracking().CountAsync(o => o.OrderStatus == 3);
            dict["CountSuccess"] = await dbContext.Order.AsNoTracking().CountAsync(o => o.OrderStatus == 4);
            dict["CountFailed"] = await dbContext.Order.AsNoTracking().CountAsync(o => o.OrderStatus == 5);
            return dict;
        }

        public async Task<bool> ApproveOrderAsync(int id)
        {
            var order = await dbContext.Order.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            order.OrderStatus = 1;
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int id)
        {
            var order = await dbContext.Order.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            if (order.OrderDetails != null)
            {
                foreach (var item in order.OrderDetails)
                {
                    var product = await dbContext.Product.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        product.Quantity += item.Quantity;
                    }
                }
            }

            order.OrderStatus = 5;
            await dbContext.SaveChangesAsync();

            // Tự động hoàn tiền vào Ví cá nhân nếu đơn đã thanh toán VietQR / Ngân hàng
            try
            {
                var walletSvc = new WalletService(dbContext);
                await walletSvc.RefundOrderToWalletAsync(order.Id, "Hủy đơn hàng");
            }
            catch { }

            return true;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await dbContext.Order.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                dbContext.OrderDetail.RemoveRange(order.OrderDetails);
            }

            dbContext.Order.Remove(order);
            await dbContext.SaveChangesAsync();
            return true;
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
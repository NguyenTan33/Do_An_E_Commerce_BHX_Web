using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Do_An_E_Commerce_BHX.Controllers;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class OrderCheckoutService : IOrderCheckoutService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        public OrderCheckoutService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? new ApplicationDbContext();
            _cartService = new CartService(_dbContext);
            _orderService = new OrderService(_dbContext, new Calculate(), _cartService);
        }

        public async Task<(Cart CartData, List<UserAddress> UserAddresses, string UserFullName, string UserPhone, int LoyaltyPoints, List<VoucherEvaluationResult> SuggestedVouchers, decimal DiscountAmount, string AppliedCode, string CouponMessage)>
            GetCheckoutDataAsync(string userId, string selectedIds, string coupon)
        {
            var cart = _cartService.GetCartByUserId(userId);
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return (null, null, null, null, 0, null, 0, "", "");
            }

            var selectedProductIds = new List<int>();
            if (!string.IsNullOrEmpty(selectedIds))
            {
                selectedProductIds = selectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(int.Parse)
                                                .ToList();
            }

            if (selectedProductIds.Any())
            {
                cart.CartDetails = cart.CartDetails.Where(cd => selectedProductIds.Contains(cd.ProductId)).ToList();
            }

            List<UserAddress> userAddresses = null;
            string userFullName = null;
            string userPhone = null;
            int loyaltyPoints = 0;

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                userAddresses = await _dbContext.UserAddresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ToListAsync();

                userFullName = user.FullName;
                userPhone = user.PhoneNumber;
                loyaltyPoints = user.LoyaltyPoints;
            }

            var voucherService = new VoucherService(_dbContext);
            var cartItems = cart != null && cart.CartDetails != null ? cart.CartDetails.ToList() : new List<CartDetail>();
            var suggestedVouchers = voucherService.GetSuggestedVouchersForCart(cartItems, userId);

            decimal discountAmount = 0;
            string appliedCode = "";
            string couponMessage = "";

            if (!string.IsNullOrWhiteSpace(coupon))
            {
                var codeUpper = coupon.Trim().ToUpper();
                var promo = await _dbContext.Promotion.Include("Category").FirstOrDefaultAsync(p => p.Code.ToUpper() == codeUpper);
                if (promo != null)
                {
                    var eval = voucherService.EvaluateVoucher(promo, cartItems, userId);
                    if (eval.IsEligible)
                    {
                        discountAmount = (decimal)eval.CalculatedDiscount;
                        appliedCode = promo.Code;
                    }
                    else
                    {
                        couponMessage = eval.ReasonIfNotEligible;
                    }
                }
                else
                {
                    couponMessage = $"Mã giảm giá '{coupon}' không tồn tại trên hệ thống!";
                }
            }

            return (cart, userAddresses, userFullName, userPhone, loyaltyPoints, suggestedVouchers, discountAmount, appliedCode, couponMessage);
        }

        public (bool Success, string Message, int OrderId, bool IsPendingSession, PendingCheckoutSession PendingSession)
            CreatePendingCheckoutSession(string userId, string receiverName, string receiverPhone, string shippingAddress,
                int paymentMethod, decimal shippingFee, decimal discountAmount, int usedPoints, string note, string selectedIds, string couponCode)
        {
            var cart = _cartService.GetCartByUserId(userId);
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return (false, "Giỏ hàng trống hoặc phiên làm việc đã hết hạn!", 0, false, null);
            }

            string pendingCode = "P" + DateTime.Now.ToString("yyMMddHHmmss");
            var pendingSession = new PendingCheckoutSession
            {
                PendingCode = pendingCode,
                ReceiverName = receiverName,
                ReceiverPhone = receiverPhone,
                ShippingAddress = shippingAddress,
                PaymentMethod = paymentMethod,
                ShippingFee = shippingFee,
                DiscountAmount = discountAmount,
                UsedPoints = usedPoints,
                Note = note,
                SelectedIds = selectedIds,
                CouponCode = couponCode,
                CreatedAt = DateTime.Now
            };

            return (true, "Chuyển sang bước chọn phương thức thanh toán!", 0, true, pendingSession);
        }

        public Order GetPendingOrderForPaymentView(PendingCheckoutSession pendingSession, string userId)
        {
            if (pendingSession == null) return null;

            var cart = _cartService.GetCartByUserId(userId);
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any()) return null;

            var selectedProductIds = new List<int>();
            if (!string.IsNullOrEmpty(pendingSession.SelectedIds))
            {
                selectedProductIds = pendingSession.SelectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(int.Parse)
                                                .ToList();
            }

            var itemsToOrder = cart.CartDetails.ToList();
            if (selectedProductIds.Any())
            {
                itemsToOrder = itemsToOrder.Where(cd => selectedProductIds.Contains(cd.ProductId)).ToList();
            }

            decimal rawTotal = _orderService.calculate.CalculatePrice(itemsToOrder);
            decimal finalTotal = rawTotal - pendingSession.DiscountAmount + pendingSession.ShippingFee - (decimal)(pendingSession.UsedPoints * 10);
            if (finalTotal < 0) finalTotal = 0;

            return new Order
            {
                Id = 0,
                OrderDate = pendingSession.CreatedAt,
                TotalAmount = Convert.ToDouble(finalTotal),
                DiscountAmount = Convert.ToDouble(pendingSession.DiscountAmount),
                ShippingFee = Convert.ToDouble(pendingSession.ShippingFee),
                OrderStatus = 0,
                PaymentMethod = pendingSession.PaymentMethod,
                PaymentStatus = 0,
                ReceiverName = pendingSession.ReceiverName,
                ReceiverPhone = pendingSession.ReceiverPhone,
                ShippingAddress = pendingSession.ShippingAddress,
                UsedPoints = pendingSession.UsedPoints,
                Note = pendingSession.PendingCode,
                OrderDetails = itemsToOrder.Select(item => new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = Convert.ToDouble(item.Product != null ? item.Product.Price : 0),
                    Product = item.Product
                }).ToList()
            };
        }

        public (bool Success, string Message, int CreatedOrderId)
            ProcessCODCheckout(string userId, int orderId, PendingCheckoutSession pendingSession)
        {
            if (orderId > 0)
            {
                var order = _dbContext.Order.FirstOrDefault(o => o.Id == orderId);
                if (order != null)
                {
                    order.PaymentMethod = 0;
                    order.PaymentStatus = 0;
                    order.OrderStatus = 0;
                    _dbContext.SaveChanges();

                    var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                    string userEmail = GetLoggedUserEmail(fullOrder ?? order);
                    string msgText = "Đặt hàng thành công với phương thức COD!";

                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? order, userEmail);
                        msgText = "Đặt hàng thành công! Hóa đơn điện tử đã được phát hành và gửi tới email " + userEmail;
                    }

                    return (true, msgText, orderId);
                }
            }

            if (pendingSession != null)
            {
                Promotion couponObj = null;
                if (!string.IsNullOrWhiteSpace(pendingSession.CouponCode))
                {
                    var codeUpper = pendingSession.CouponCode.Trim().ToUpper();
                    couponObj = _dbContext.Promotion.FirstOrDefault(p => p.Code.ToUpper() == codeUpper && p.IsActive);
                }

                var selectedProductIds = new List<int>();
                if (!string.IsNullOrEmpty(pendingSession.SelectedIds))
                {
                    selectedProductIds = pendingSession.SelectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(int.Parse)
                                                    .ToList();
                }

                var createdOrder = _orderService.CreateOrder(
                    userId: userId,
                    receiverName: pendingSession.ReceiverName,
                    receiverPhone: pendingSession.ReceiverPhone,
                    shippingAddress: pendingSession.ShippingAddress,
                    coupon: couponObj,
                    paymentMethod: 0,
                    shippingFee: pendingSession.ShippingFee,
                    discountAmount: pendingSession.DiscountAmount,
                    usedPoints: pendingSession.UsedPoints,
                    note: pendingSession.Note,
                    selectedProductIds: selectedProductIds
                );

                var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == createdOrder.Id);
                string userEmail = GetLoggedUserEmail(fullOrder ?? createdOrder);
                string msgText = "Đặt hàng COD thành công!";

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? createdOrder, userEmail);
                    msgText = "Đặt hàng COD thành công! Hóa đơn điện tử đã được gửi tới email " + userEmail;
                }

                return (true, msgText, createdOrder.Id);
            }

            return (false, "Không tìm thấy phiên thông tin đặt hàng!", 0);
        }

        public (bool Success, bool IsPaid, string Message, int CreatedOrderId)
            ConfirmBankPayment(string userId, int orderId, int paymentMethod, PendingCheckoutSession pendingSession)
        {
            if (orderId > 0)
            {
                var order = _dbContext.Order.FirstOrDefault(o => o.Id == orderId);
                if (order != null)
                {
                    order.PaymentMethod = paymentMethod;
                    order.PaymentStatus = 1;
                    order.OrderStatus = 0;
                    _dbContext.SaveChanges();

                    var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                    string userEmail = GetLoggedUserEmail(fullOrder ?? order);
                    string methodText = paymentMethod == 2 ? "Ví MoMo" : "Chuyển khoản Ngân hàng";
                    string msgText = $"Thanh toán qua {methodText} thành công!";

                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? order, userEmail);
                        msgText = $"Thanh toán qua {methodText} thành công! Hóa đơn điện tử đã được phát hành và gửi tới email " + userEmail;
                    }

                    return (true, true, msgText, orderId);
                }
            }

            if (pendingSession != null)
            {
                Promotion couponObj = null;
                if (!string.IsNullOrWhiteSpace(pendingSession.CouponCode))
                {
                    var codeUpper = pendingSession.CouponCode.Trim().ToUpper();
                    couponObj = _dbContext.Promotion.FirstOrDefault(p => p.Code.ToUpper() == codeUpper && p.IsActive);
                }

                var selectedProductIds = new List<int>();
                if (!string.IsNullOrEmpty(pendingSession.SelectedIds))
                {
                    selectedProductIds = pendingSession.SelectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(int.Parse)
                                                    .ToList();
                }

                var createdOrder = _orderService.CreateOrder(
                    userId: userId,
                    receiverName: pendingSession.ReceiverName,
                    receiverPhone: pendingSession.ReceiverPhone,
                    shippingAddress: pendingSession.ShippingAddress,
                    coupon: couponObj,
                    paymentMethod: paymentMethod,
                    shippingFee: pendingSession.ShippingFee,
                    discountAmount: pendingSession.DiscountAmount,
                    usedPoints: pendingSession.UsedPoints,
                    note: pendingSession.Note,
                    selectedProductIds: selectedProductIds
                );

                createdOrder.PaymentStatus = 1;
                createdOrder.OrderStatus = 0;
                _dbContext.SaveChanges();

                var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == createdOrder.Id);
                string userEmail = GetLoggedUserEmail(fullOrder ?? createdOrder);
                string methodText = paymentMethod == 2 ? "Ví MoMo" : "Chuyển khoản VietQR";
                string msgText = $"Thanh toán {methodText} thành công!";

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    try
                    {
                        OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? createdOrder, userEmail);
                        msgText = $"Thanh toán {methodText} thành công! Hóa đơn điện tử đã được phát hành và gửi tới email " + userEmail;
                    }
                    catch (Exception emailEx)
                    {
                        LogSePay("[LỖI GỬI EMAIL HÓA ĐƠN] " + emailEx.Message);
                    }
                }

                return (true, true, msgText, createdOrder.Id);
            }

            return (false, false, "Không tìm thấy phiên thông tin thanh toán!", 0);
        }

        public (bool IsPaid, bool IsExpired, int PaymentStatus, int PaymentMethod, string Message)
            CheckPaymentStatus(string userId, int orderId, PendingCheckoutSession pendingSession, int? lastCreatedOrderId)
        {
            if (orderId > 0)
            {
                var order = _dbContext.Order.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return (false, false, 0, 0, "Không tìm thấy đơn");
                }

                if (order.PaymentStatus == 0 && order.PaymentMethod == 1 && (DateTime.Now - order.OrderDate).TotalMinutes >= 10)
                {
                    AutoCancelExpiredOrders();
                    return (false, true, 0, 1, "Đơn hàng đã hết hạn thanh toán (quá 10 phút)!");
                }

                if (order.PaymentStatus == 1)
                {
                    return (true, false, 1, order.PaymentMethod, "Đã thanh toán");
                }

                bool isPaidFromSePay = CheckSePayApiForOrder(order);
                if (isPaidFromSePay)
                {
                    order.PaymentMethod = 1;
                    order.PaymentStatus = 1;
                    order.OrderStatus = 0;
                    _dbContext.SaveChanges();

                    var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                    string userEmail = GetLoggedUserEmail(fullOrder ?? order);
                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        try { OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? order, userEmail); } catch { }
                    }

                    return (true, false, 1, 1, "Thanh toán thành công (Xác thực tự động từ SePay REST API)!");
                }

                return (false, false, order.PaymentStatus, order.PaymentMethod, "Chưa thanh toán");
            }

            if (pendingSession != null)
            {
                if ((DateTime.Now - pendingSession.CreatedAt).TotalMinutes >= 10)
                {
                    return (false, true, 0, 1, "Phiên thanh toán đã hết hạn 10 phút!");
                }

                bool isPaidFromSePay = CheckSePayApiForPending(pendingSession);
                if (isPaidFromSePay)
                {
                    return (true, false, 1, 1, "Thanh toán phiên chờ thành công!");
                }
            }
            else if (lastCreatedOrderId.HasValue && lastCreatedOrderId.Value > 0)
            {
                return (true, false, 1, 1, "Thanh toán thành công!");
            }

            return (false, false, 0, 0, "Chưa thanh toán");
        }

        private string GetLoggedUserEmail(Order order)
        {
            if (order != null && order.User != null)
            {
                if (!string.IsNullOrWhiteSpace(order.User.Email) && order.User.Email.Contains("@")) return order.User.Email;
                if (!string.IsNullOrWhiteSpace(order.User.UserName) && order.User.UserName.Contains("@")) return order.User.UserName;
            }
            return null;
        }

        private bool CheckSePayApiForPending(PendingCheckoutSession pending)
        {
            if (pending == null) return false;
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string apiKey = System.Configuration.ConfigurationManager.AppSettings["SePay_ApiKey"] ?? "";
                if (string.IsNullOrWhiteSpace(apiKey)) return false;

                string url = "https://my.sepay.vn/userapi/transactions/list";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + apiKey;
                request.ContentType = "application/json";
                request.Timeout = 6000;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json)) return false;

                    var jObj = JObject.Parse(json);
                    var txs = jObj["transactions"] as JArray;

                    if (txs != null && txs.Count > 0)
                    {
                        string pCode = (pending.PendingCode ?? "").ToUpper();
                        string pCodeNoP = pCode.Replace("P", "");
                        string phone = (pending.ReceiverPhone ?? "").Trim();

                        foreach (var item in txs)
                        {
                            string content = (
                                (item["transaction_content"]?.ToString() ?? "") + " " +
                                (item["content"]?.ToString() ?? "") + " " +
                                (item["description"]?.ToString() ?? "") + " " +
                                (item["code"]?.ToString() ?? "") + " " +
                                (item["reference_number"]?.ToString() ?? "")
                            ).ToUpper();

                            string strAmount = item["amount_in"]?.ToString() ?? item["amountIn"]?.ToString() ?? "0";
                            double amountIn = 0;
                            double.TryParse(strAmount, out amountIn);

                            if (!string.IsNullOrEmpty(content) || amountIn > 0)
                            {
                                if (!string.IsNullOrEmpty(pCode) && (content.Contains(pCode) || content.Contains(pCodeNoP)))
                                {
                                    LogSePay($"[SEPAY API HIT PENDING THÀNH CÔNG] Khớp mã phiên {pCode}: Content='{content}', Amount={amountIn}");
                                    return true;
                                }

                                if (!string.IsNullOrEmpty(phone) && phone.Length >= 8 && content.Contains(phone))
                                {
                                    LogSePay($"[SEPAY API HIT PENDING THÀNH CÔNG] Khớp SĐT người nhận {phone}: Content='{content}', Amount={amountIn}");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogSePay("[LỖI GỌI SEPAY REST API FOR PENDING] " + ex.Message);
            }
            return false;
        }

        private bool CheckSePayApiForOrder(Order order)
        {
            if (order == null) return false;
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string apiKey = System.Configuration.ConfigurationManager.AppSettings["SePay_ApiKey"] ?? "";
                if (string.IsNullOrWhiteSpace(apiKey)) return false;

                string url = "https://my.sepay.vn/userapi/transactions/list";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + apiKey;
                request.ContentType = "application/json";
                request.Timeout = 5000;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json)) return false;

                    var jObj = JObject.Parse(json);
                    var txs = jObj["transactions"] as JArray;

                    if (txs != null && txs.Count > 0)
                    {
                        string orderIdStr = order.Id.ToString();
                        string pattern1 = "BHX" + orderIdStr;
                        string pattern2 = "BHX " + orderIdStr;
                        string pattern3 = "BHX_" + orderIdStr;
                        string pattern4 = "BHX-" + orderIdStr;

                        foreach (var item in txs)
                        {
                            string strAmount = item["amount_in"]?.ToString() ?? item["amountIn"]?.ToString() ?? "0";
                            double amountIn = 0;
                            double.TryParse(strAmount, out amountIn);

                            string content = (item["transaction_content"]?.ToString() ?? item["content"]?.ToString() ?? item["description"]?.ToString() ?? "").ToUpper();

                            if (amountIn > 0 || !string.IsNullOrEmpty(content))
                            {
                                if (content.Contains(pattern1) || 
                                    content.Contains(pattern2) || 
                                    content.Contains(pattern3) || 
                                    content.Contains(pattern4) ||
                                    (content.Contains("BHX") && content.Contains(orderIdStr)) ||
                                    (content.Contains("SEVQR") && content.Contains(orderIdStr)) ||
                                    content.Contains(orderIdStr))
                                {
                                    LogSePay($"[SEPAY API HIT THÀNH CÔNG] Tìm thấy giao dịch SePay khớp Đơn #{order.Id}: Content='{content}', Amount={amountIn}");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogSePay("[LỖI GỌI SEPAY REST API] " + ex.Message);
            }
            return false;
        }

        private void AutoCancelExpiredOrders()
        {
            try
            {
                var now = DateTime.Now;
                var expiredOrders = _dbContext.Order
                    .Include("OrderDetails.Product")
                    .Where(o => o.PaymentStatus == 0 && o.PaymentMethod == 1 && o.OrderStatus != 5 && o.OrderStatus != 4)
                    .ToList();

                bool hasChanges = false;
                foreach (var order in expiredOrders)
                {
                    if ((now - order.OrderDate).TotalMinutes >= 10)
                    {
                        order.OrderStatus = 5;
                        if (order.OrderDetails != null)
                        {
                            foreach (var detail in order.OrderDetails)
                            {
                                if (detail.Product != null)
                                {
                                    detail.Product.Quantity += detail.Quantity;
                                }
                            }
                        }
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    _dbContext.SaveChanges();
                }
            }
            catch { }
        }

        public void LogSePay(string message)
        {
            try
            {
                string logPath = HostingEnvironment.MapPath("~/App_Data/sepay_transactions.log");
                if (string.IsNullOrEmpty(logPath))
                {
                    logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "sepay_transactions.log");
                }
                string dir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n", Encoding.UTF8);
            }
            catch { }
        }
    }
}

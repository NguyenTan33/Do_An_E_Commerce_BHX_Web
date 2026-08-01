using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Models; // Thêm namespace chứa các Service của ông

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class PendingCheckoutSession
    {
        public string PendingCode { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ShippingAddress { get; set; }
        public int PaymentMethod { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public int UsedPoints { get; set; }
        public string Note { get; set; }
        public string SelectedIds { get; set; }
        public string CouponCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class OrderController : BaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;

        public OrderController()
        {
            _dbContext = new ApplicationDbContext();
            _cartService = new CartService(_dbContext);
            _orderService = new OrderService(_dbContext, new Calculate(), _cartService);
        }

        // GET: /Order/Index (Trang Thanh Toán)
        [HttpGet]
        public ActionResult Index(string selectedIds = null, string coupon = null)
        {
            string userId = GetCurrentUserId();
            var cart = _cartService.GetCartByUserId(userId);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Nếu truyền selectedIds thì lọc danh sách các món chọn
            var selectedProductIds = new System.Collections.Generic.List<int>();
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

            // Lấy thông tin User đã đăng nhập & Danh sách địa chỉ đã lưu
            bool isAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAuthenticated = isAuthenticated;

            if (isAuthenticated)
            {
                var userAddresses = _dbContext.UserAddresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ToList();
                ViewBag.UserAddresses = userAddresses;

                var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    ViewBag.UserFullName = user.FullName;
                    ViewBag.UserPhone = user.PhoneNumber;
                    ViewBag.LoyaltyPoints = user.LoyaltyPoints;
                }
                else
                {
                    ViewBag.LoyaltyPoints = 0;
                }
            }
            else
            {
                ViewBag.LoyaltyPoints = 0;
            }

            // Xử lý mã giảm giá Coupon qua VoucherService Shopee-style
            var voucherService = new VoucherService(_dbContext);
            var cartItems = cart != null && cart.CartDetails != null ? cart.CartDetails.ToList() : new System.Collections.Generic.List<CartDetail>();
            var suggestedVouchers = voucherService.GetSuggestedVouchersForCart(cartItems, userId);
            ViewBag.SuggestedVouchers = suggestedVouchers;

            decimal discountAmount = 0;
            string appliedCode = "";
            string couponMessage = "";

            if (!string.IsNullOrWhiteSpace(coupon))
            {
                var codeUpper = coupon.Trim().ToUpper();
                var promo = _dbContext.Promotion.Include("Category").FirstOrDefault(p => p.Code.ToUpper() == codeUpper);
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

            ViewBag.CouponCode = appliedCode;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.CouponMessage = couponMessage;
            ViewBag.SelectedIds = selectedIds ?? "";

            return View(cart);
        }

        // POST: /Order/Checkout (Xử lý chốt đơn qua AJAX)
        [HttpPost]
        public ActionResult Checkout(string receiverName, string receiverPhone, string shippingAddress, int paymentMethod = 0, decimal shippingFee = 0, decimal discountAmount = 0, int usedPoints = 0, string note = "", string selectedIds = "", string couponCode = "")
        {
            string userId = GetCurrentUserId();

            try
            {
                var selectedProductIds = new System.Collections.Generic.List<int>();
                if (!string.IsNullOrEmpty(selectedIds))
                {
                    selectedProductIds = selectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(int.Parse)
                                                    .ToList();
                }

                // Kiểm tra giỏ hàng có đủ sản phẩm
                var cart = _cartService.GetCartByUserId(userId);
                if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống hoặc phiên làm việc đã hết hạn!" });
                }

                Promotion couponObj = null;
                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    var codeUpper = couponCode.Trim().ToUpper();
                    couponObj = _dbContext.Promotion.FirstOrDefault(p => p.Code.ToUpper() == codeUpper && p.IsActive);
                }

                // LƯU PHIÊN THANH TOÁN TẠM THỜI VÀO SESSION -> CHƯA TẠO ORDER DB, TUYỆT ĐỐI KHÔNG TRỪ TỒN KHO!
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

                Session["PendingCheckoutSession"] = pendingSession;

                return Json(new {
                    success = true,
                    message = "Chuyển sang bước chọn phương thức thanh toán!",
                    orderId = 0,
                    isPendingSession = true,
                    redirectUrl = Url.Action("Payment", "Order", new { isPending = 1 })
                });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                var fullErrorMessage = string.Join(" | ", errorMessages);

                return Json(new { success = false, message = "LỖI TẠI CỘT: " + fullErrorMessage });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var current = ex.InnerException;
                while (current != null)
                {
                    msg += " -> " + current.Message;
                    current = current.InnerException;
                }
                return Json(new { success = false, message = "LỖI ĐẶT HÀNG: " + msg });
            }
        }

        // GET: /Order/Payment?orderId=123 hoặc ?isPending=1 (Trang quét mã VietQR)
        [HttpGet]
        public ActionResult Payment(int orderId = 0, int isPending = 0)
        {
            if (orderId > 0)
            {
                var order = _dbContext.Order
                    .Include("OrderDetails.Product")
                    .FirstOrDefault(o => o.Id == orderId);

                if (order != null)
                {
                    return View(order);
                }
            }

            // Dựng giao diện VietQR tạm thời từ Session đối với đơn chưa chốt (Chưa tạo DB, Chưa trừ kho)
            var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
            if (pending != null)
            {
                string userId = GetCurrentUserId();
                var cart = _cartService.GetCartByUserId(userId);
                if (cart != null && cart.CartDetails != null && cart.CartDetails.Any())
                {
                    var selectedProductIds = new System.Collections.Generic.List<int>();
                    if (!string.IsNullOrEmpty(pending.SelectedIds))
                    {
                        selectedProductIds = pending.SelectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(int.Parse)
                                                        .ToList();
                    }

                    var itemsToOrder = cart.CartDetails.ToList();
                    if (selectedProductIds.Any())
                    {
                        itemsToOrder = itemsToOrder.Where(cd => selectedProductIds.Contains(cd.ProductId)).ToList();
                    }

                    decimal rawTotal = _orderService.calculate.CalculatePrice(itemsToOrder);
                    decimal finalTotal = rawTotal - pending.DiscountAmount + pending.ShippingFee - (decimal)(pending.UsedPoints * 10);
                    if (finalTotal < 0) finalTotal = 0;

                    var pendingOrderModel = new Order
                    {
                        Id = 0, // Id = 0 đại diện cho phiên chờ thanh toán chưa tạo DB
                        OrderDate = pending.CreatedAt,
                        TotalAmount = Convert.ToDouble(finalTotal),
                        DiscountAmount = Convert.ToDouble(pending.DiscountAmount),
                        ShippingFee = Convert.ToDouble(pending.ShippingFee),
                        OrderStatus = 0,
                        PaymentMethod = pending.PaymentMethod,
                        PaymentStatus = 0,
                        ReceiverName = pending.ReceiverName,
                        ReceiverPhone = pending.ReceiverPhone,
                        ShippingAddress = pending.ShippingAddress,
                        UsedPoints = pending.UsedPoints,
                        Note = pending.PendingCode,
                        OrderDetails = itemsToOrder.Select(item => new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = Convert.ToDouble(item.Product != null ? item.Product.Price : 0),
                            Product = item.Product
                        }).ToList()
                    };

                    return View(pendingOrderModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        private string GetLoggedUserEmail(Order order)
        {
            if (order != null && order.User != null)
            {
                if (!string.IsNullOrWhiteSpace(order.User.Email) && order.User.Email.Contains("@")) return order.User.Email;
                if (!string.IsNullOrWhiteSpace(order.User.UserName) && order.User.UserName.Contains("@")) return order.User.UserName;
            }

            if (User.Identity.IsAuthenticated)
            {
                string currentName = User.Identity.Name;
                if (!string.IsNullOrWhiteSpace(currentName) && currentName.Contains("@")) return currentName;

                string currentId = User.Identity.GetUserId();
                if (!string.IsNullOrWhiteSpace(currentId))
                {
                    var u = _dbContext.Users.FirstOrDefault(x => x.Id == currentId);
                    if (u != null)
                    {
                        if (!string.IsNullOrWhiteSpace(u.Email) && u.Email.Contains("@")) return u.Email;
                        if (!string.IsNullOrWhiteSpace(u.UserName) && u.UserName.Contains("@")) return u.UserName;
                    }
                }
            }

            return null;
        }

        // POST: /Order/ProcessCOD (Xử lý hoàn tất đặt hàng COD)
        [HttpPost]
        public ActionResult ProcessCOD(int orderId = 0)
        {
            try
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

                        return Json(new { 
                            success = true, 
                            message = msgText, 
                            redirectUrl = Url.Action("Success", "Order", new { orderId = orderId, paymentMethod = 0 }) 
                        });
                    }
                }

                // Nếu chốt COD từ phiên chờ Session
                var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
                if (pending != null)
                {
                    string userId = GetCurrentUserId();
                    Promotion couponObj = null;
                    if (!string.IsNullOrWhiteSpace(pending.CouponCode))
                    {
                        var codeUpper = pending.CouponCode.Trim().ToUpper();
                        couponObj = _dbContext.Promotion.FirstOrDefault(p => p.Code.ToUpper() == codeUpper && p.IsActive);
                    }

                    var selectedProductIds = new System.Collections.Generic.List<int>();
                    if (!string.IsNullOrEmpty(pending.SelectedIds))
                    {
                        selectedProductIds = pending.SelectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(int.Parse)
                                                        .ToList();
                    }

                    // TẠO ORDER DB + MỚI TRỪ TỒN KHO + XÓA GIỎ HÀNG THỰC TẾ
                    var createdOrder = _orderService.CreateOrder(
                        userId: userId,
                        receiverName: pending.ReceiverName,
                        receiverPhone: pending.ReceiverPhone,
                        shippingAddress: pending.ShippingAddress,
                        coupon: couponObj,
                        paymentMethod: 0,
                        shippingFee: pending.ShippingFee,
                        discountAmount: pending.DiscountAmount,
                        usedPoints: pending.UsedPoints,
                        note: pending.Note,
                        selectedProductIds: selectedProductIds
                    );

                    Session.Remove("PendingCheckoutSession");

                    var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == createdOrder.Id);
                    string userEmail = GetLoggedUserEmail(fullOrder ?? createdOrder);
                    string msgText = "Đặt hàng COD thành công!";

                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? createdOrder, userEmail);
                        msgText = "Đặt hàng COD thành công! Hóa đơn điện tử đã được gửi tới email " + userEmail;
                    }

                    return Json(new {
                        success = true,
                        message = msgText,
                        redirectUrl = Url.Action("Success", "Order", new { orderId = createdOrder.Id, paymentMethod = 0 })
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy phiên thông tin đặt hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xử lý COD: " + ex.Message });
            }
        }

        // POST: /Order/ConfirmBankPaymentApi (Xác nhận thanh toán chuyển khoản / MoMo thành công)
        [HttpPost]
        public ActionResult ConfirmBankPaymentApi(int orderId = 0, int paymentMethod = 1)
        {
            try
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

                        return Json(new { 
                            success = true, 
                            message = msgText, 
                            redirectUrl = Url.Action("Success", "Order", new { orderId = orderId, paymentMethod = paymentMethod }) 
                        });
                    }
                }

                // Nếu xác nhận chuyển khoản thành công cho phiên chờ Session
                var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
                if (pending != null)
                {
                    string userId = GetCurrentUserId();
                    Promotion couponObj = null;
                    if (!string.IsNullOrWhiteSpace(pending.CouponCode))
                    {
                        var codeUpper = pending.CouponCode.Trim().ToUpper();
                        couponObj = _dbContext.Promotion.FirstOrDefault(p => p.Code.ToUpper() == codeUpper && p.IsActive);
                    }

                    var selectedProductIds = new System.Collections.Generic.List<int>();
                    if (!string.IsNullOrEmpty(pending.SelectedIds))
                    {
                        selectedProductIds = pending.SelectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(int.Parse)
                                                        .ToList();
                    }

                    // TẠO ORDER DB + TỰ ĐỘNG ĐÁNH DẤU ĐÃ THANH TOÁN + MỚI TRỪ TỒN KHO + XÓA GIỎ
                    var createdOrder = _orderService.CreateOrder(
                        userId: userId,
                        receiverName: pending.ReceiverName,
                        receiverPhone: pending.ReceiverPhone,
                        shippingAddress: pending.ShippingAddress,
                        coupon: couponObj,
                        paymentMethod: paymentMethod,
                        shippingFee: pending.ShippingFee,
                        discountAmount: pending.DiscountAmount,
                        usedPoints: pending.UsedPoints,
                        note: pending.Note,
                        selectedProductIds: selectedProductIds
                    );

                    createdOrder.PaymentStatus = 1; // Đã thanh toán thành công
                    createdOrder.OrderStatus = 0;   // Chờ duyệt
                    _dbContext.SaveChanges();

                    Session.Remove("PendingCheckoutSession");

                    var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == createdOrder.Id);
                    string userEmail = GetLoggedUserEmail(fullOrder ?? createdOrder);
                    string methodText = paymentMethod == 2 ? "Ví MoMo" : "Chuyển khoản VietQR";
                    string msgText = $"Thanh toán {methodText} thành công!";

                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? createdOrder, userEmail);
                        msgText = $"Thanh toán {methodText} thành công! Hóa đơn điện tử đã được phát hành và gửi tới email " + userEmail;
                    }

                    return Json(new {
                        success = true,
                        message = msgText,
                        redirectUrl = Url.Action("Success", "Order", new { orderId = createdOrder.Id, paymentMethod = paymentMethod })
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy phiên thông tin thanh toán!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xác nhận thanh toán: " + ex.Message });
            }
        }

        // GET: /Order/Success (Trang thông báo hoàn tất)
        [HttpGet]
        public ActionResult Success(int paymentMethod = 0, int orderId = 0)
        {
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.OrderId = orderId;

            if (orderId > 0)
            {
                var order = _dbContext.Order
                    .Include("OrderDetails.Product")
                    .FirstOrDefault(o => o.Id == orderId);
                ViewBag.Order = order;
            }

            return View();
        }

        // GET: /Order/CheckPaymentStatus?orderId=123 (Auto-Polling gọi trực tiếp SePay REST API)
        [HttpGet]
        public ActionResult CheckPaymentStatus(int orderId = 0)
        {
            if (orderId > 0)
            {
                var order = _dbContext.Order.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return Json(new { isPaid = false, message = "Không tìm thấy đơn" }, JsonRequestBehavior.AllowGet);
                }

                // 0. Kiểm tra nếu đơn hàng chuyển khoản đã quá hạn 10 phút mà chưa thanh toán
                if (order.PaymentStatus == 0 && order.PaymentMethod == 1 && (DateTime.Now - order.OrderDate).TotalMinutes >= 10)
                {
                    AutoCancelExpiredOrders();
                    return Json(new { 
                        isPaid = false, 
                        isExpired = true, 
                        message = "Đơn hàng đã hết hạn thanh toán (quá 10 phút)!",
                        orderId = orderId
                    }, JsonRequestBehavior.AllowGet);
                }

                // 1. Nếu đơn đã ở trạng thái ĐÃ THANH TOÁN (1)
                if (order.PaymentStatus == 1)
                {
                    return Json(new { 
                        isPaid = true, 
                        paymentStatus = 1, 
                        paymentMethod = order.PaymentMethod,
                        orderId = orderId
                    }, JsonRequestBehavior.AllowGet);
                }

                // 2. TỰ ĐỘNG CHỦ ĐỘNG GỌI SEPAY REST API TỪ LOCALHOST
                bool isPaidFromSePay = CheckSePayApiForOrder(order);
                if (isPaidFromSePay)
                {
                    order.PaymentMethod = 1; // VietinBank / SePay
                    order.PaymentStatus = 1; // Đã thanh toán thành công
                    order.OrderStatus = 0;   // Chờ duyệt / chuẩn bị hàng
                    _dbContext.SaveChanges();

                    // Lấy thông tin order đầy đủ để gửi Email Hóa đơn
                    var fullOrder = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                    string userEmail = GetLoggedUserEmail(fullOrder ?? order);
                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        OrderInvoiceEmailService.SendOrderConfirmationEmail(fullOrder ?? order, userEmail);
                    }

                    return Json(new { 
                        isPaid = true, 
                        paymentStatus = 1, 
                        paymentMethod = 1,
                        orderId = orderId,
                        message = "Thanh toán thành công (Xác thực tự động từ SePay REST API)!"
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { 
                    isPaid = false, 
                    paymentStatus = order.PaymentStatus, 
                    paymentMethod = order.PaymentMethod,
                    orderId = orderId
                }, JsonRequestBehavior.AllowGet);
            }

            // Nếu orderId == 0: Kiểm tra VietQR tự động cho phiên chờ Session chưa tạo DB
            var pending = Session["PendingCheckoutSession"] as PendingCheckoutSession;
            if (pending != null)
            {
                if ((DateTime.Now - pending.CreatedAt).TotalMinutes >= 10)
                {
                    Session.Remove("PendingCheckoutSession");
                    return Json(new { isPaid = false, isExpired = true, message = "Phiên thanh toán đã hết hạn 10 phút!" }, JsonRequestBehavior.AllowGet);
                }

                bool isPaidFromSePay = CheckSePayApiForPending(pending);
                if (isPaidFromSePay)
                {
                    // TẠO ORDER DB + TỰ ĐỘNG ĐÁNH DẤU ĐÃ THANH TOÁN + MỚI TRỪ TỒN KHO + XÓA GIỎ
                    return ConfirmBankPaymentApi(0, 1);
                }
            }

            return Json(new { isPaid = false }, JsonRequestBehavior.AllowGet);
        }

        private bool CheckSePayApiForPending(PendingCheckoutSession pending)
        {
            if (pending == null) return false;
            try
            {
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

                string apiKey = System.Configuration.ConfigurationManager.AppSettings["SePay_ApiKey"] ?? "";
                if (string.IsNullOrWhiteSpace(apiKey)) return false;

                string url = "https://my.sepay.vn/userapi/transactions/list";
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + apiKey;
                request.ContentType = "application/json";
                request.Timeout = 5000;

                using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json)) return false;

                    var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var txs = jObj["transactions"] as Newtonsoft.Json.Linq.JArray;

                    if (txs != null && txs.Count > 0)
                    {
                        string pCode = (pending.PendingCode ?? "").ToUpper();
                        string phone = (pending.ReceiverPhone ?? "").Trim();

                        foreach (var item in txs)
                        {
                            string content = (item["transaction_content"]?.ToString() ?? item["content"]?.ToString() ?? item["description"]?.ToString() ?? "").ToUpper();

                            if (!string.IsNullOrEmpty(content))
                            {
                                if ((!string.IsNullOrEmpty(pCode) && content.Contains(pCode)) ||
                                    (!string.IsNullOrEmpty(phone) && phone.Length >= 8 && content.Contains(phone)))
                                {
                                    LogSePay($"[SEPAY API HIT SESSION THÀNH CÔNG] Khớp phiên {pCode} - Phone {phone}: Content='{content}'");
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
                // 1. Kích hoạt TLS 1.2 cho .NET Framework 4.7.2 WebRequest
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

                string apiKey = System.Configuration.ConfigurationManager.AppSettings["SePay_ApiKey"] ?? "";
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    LogSePay("[SEPAY API CHECK] SePay_ApiKey trong Web.config bị rỗng.");
                    return false;
                }

                string url = "https://my.sepay.vn/userapi/transactions/list";
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + apiKey;
                request.ContentType = "application/json";
                request.Timeout = 5000; // 5 giây timeout

                using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json)) return false;

                    var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var txs = jObj["transactions"] as Newtonsoft.Json.Linq.JArray;

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
                LogSePay("[LỖI GỌI SEPAY REST API] " + ex.Message + (ex.InnerException != null ? " -> " + ex.InnerException.Message : ""));
            }
            return false;
        }

        private void LogSePay(string message)
        {
            try
            {
                string logPath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/sepay_transactions.log");
                if (string.IsNullOrEmpty(logPath))
                {
                    logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "sepay_transactions.log");
                }
                string dir = System.IO.Path.GetDirectoryName(logPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n", System.Text.Encoding.UTF8);
            }
            catch { }
        }

    public class SePayRestApiTransaction
    {
        public int id { get; set; }
        public string bank_brand_name { get; set; }
        public string account_number { get; set; }
        public string transaction_date { get; set; }
        public double amount_in { get; set; }
        public double amount_out { get; set; }
        public string transaction_content { get; set; }
        public string reference_number { get; set; }
    }

    public class SePayRestApiResponse
    {
        public int status { get; set; }
        public string[] messages { get; set; }
        public System.Collections.Generic.List<SePayRestApiTransaction> transactions { get; set; }
    }

        // GET: /Order/Track (Trang Tra cứu đơn hàng bằng SĐT hoặc Mã đơn)
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Track(string query = "")
        {
            ViewBag.InitialQuery = query;
            return View();
        }

        // POST: /Order/SearchOrderJson (AJAX tra cứu đơn hàng)
        [AllowAnonymous]
        [HttpPost]
        public ActionResult SearchOrderJson(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Vui lòng nhập Số điện thoại nhận hàng hoặc Mã đơn hàng!" });
            }

            string cleanQuery = query.Trim().Replace("#", "");
            var qOrders = _dbContext.Order.AsQueryable();

            int parsedOrderId = 0;
            bool isNumericId = int.TryParse(cleanQuery, out parsedOrderId);

            if (isNumericId && parsedOrderId > 0)
            {
                qOrders = qOrders.Where(o => o.Id == parsedOrderId || (o.ReceiverPhone != null && o.ReceiverPhone.Contains(cleanQuery)));
            }
            else
            {
                qOrders = qOrders.Where(o => o.ReceiverPhone != null && o.ReceiverPhone.Contains(cleanQuery));
            }

            var orders = qOrders
                .Include("OrderDetails.Product")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (!orders.Any())
            {
                return Json(new { success = false, message = $"Không tìm thấy đơn hàng nào phù hợp với từ khóa '{query}'!" });
            }

            // Tự động kiểm tra & hủy các đơn hàng VietQR chưa thanh toán quá 10 phút
            AutoCancelExpiredOrders();

            var orderList = orders.Select(o => new
            {
                id = o.Id,
                orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                rawOrderDate = o.OrderDate,
                receiverName = o.ReceiverName,
                receiverPhone = o.ReceiverPhone,
                shippingAddress = o.ShippingAddress,
                totalAmount = o.TotalAmount,
                discountAmount = o.DiscountAmount,
                shippingFee = o.ShippingFee,
                orderStatus = o.OrderStatus,
                orderStatusText = o.OrderStatus == 0 ? "Chờ duyệt" :
                                 o.OrderStatus == 1 ? "Đã duyệt" :
                                 o.OrderStatus == 2 ? "Đã đóng gói" :
                                 o.OrderStatus == 3 ? "Đang giao hàng" :
                                 o.OrderStatus == 4 ? "Giao thành công" : "Đã hủy",
                paymentMethod = o.PaymentMethod == 0 ? "COD (Tiền mặt)" : "Chuyển khoản VietinBank",
                paymentStatus = o.PaymentStatus == 1 ? "Đã thanh toán" : "Chưa thanh toán",
                rawPaymentStatus = o.PaymentStatus,
                rawOrderStatus = o.OrderStatus,
                isExpired = o.PaymentStatus == 0 && o.PaymentMethod == 1 && (DateTime.Now - o.OrderDate).TotalMinutes >= 10,
                paymentUrl = Url.Action("Payment", "Order", new { orderId = o.Id }),
                items = o.OrderDetails != null ? o.OrderDetails.Select(d => new
                {
                    productId = d.ProductId,
                    productName = d.Product != null ? d.Product.Name : "Sản phẩm #" + d.ProductId,
                    productImage = d.Product != null && !string.IsNullOrEmpty(d.Product.URLImage) ? d.Product.URLImage : "/Content/images/no-image.png",
                    quantity = d.Quantity,
                    price = d.Price,
                    total = d.Price * d.Quantity
                }).ToList() : null
            }).ToList();

            return Json(new { success = true, count = orderList.Count, orders = orderList });
        }

        // POST: /Order/CancelOrder (Hủy đơn hàng chưa nhận / chưa thanh toán)
        [AllowAnonymous]
        [HttpPost]
        public ActionResult CancelOrder(int orderId)
        {
            try
            {
                var order = _dbContext.Order.Include("OrderDetails.Product").FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // Không cho phép hủy đơn nếu đã giao thành công (4), đang giao hàng (3), hoặc đã hủy trước đó (5)
                if (order.OrderStatus == 3 || order.OrderStatus == 4 || order.OrderStatus == 5)
                {
                    return Json(new { success = false, message = "Đơn hàng đang giao, đã giao thành công hoặc đã hủy trước đó, không thể hủy!" });
                }

                if (order.PaymentStatus == 1)
                {
                    return Json(new { success = false, message = "Đơn hàng đã được thanh toán thành công, vui lòng liên hệ bộ phận hỗ trợ để hoàn tiền!" });
                }

                // Cập nhật trạng thái sang 5 = Đã hủy
                order.OrderStatus = 5;

                // Hoàn lại số lượng sản phẩm vào kho
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

                _dbContext.SaveChanges();

                return Json(new { success = true, message = $"Đã hủy thành công đơn hàng #{orderId}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi hủy đơn hàng: " + ex.Message });
            }
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
                        order.OrderStatus = 5; // Đã hủy tự động do quá 10 phút chưa thanh toán
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}
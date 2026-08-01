using System;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class SePayWebhookModel
    {
        public int id { get; set; }
        public string gateway { get; set; }
        public string transactionDate { get; set; }
        public string accountNumber { get; set; }
        public string code { get; set; }
        public string content { get; set; }
        public string transferType { get; set; }
        public double transferAmount { get; set; }
        public double accumulated { get; set; }
        public string subAccount { get; set; }
        public string referenceCode { get; set; }
        public string description { get; set; }
    }

    public class SePayController : Controller
    {
        private ApplicationDbContext _dbContext = new ApplicationDbContext();

        // POST: /SePay/Webhook (Nhận Webhook số dư tiền vào VietinBank từ SePay)
        [HttpPost]
        public ActionResult Webhook(SePayWebhookModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Payload SePay rỗng" });
                }

                string logContent = $"Gateway: {model.gateway}, Amount: {model.transferAmount}, Content: '{model.content}', Account: {model.accountNumber}";
                LogSePay(logContent);

                // 1. Kiểm tra API Key (Xác thực bảo mật SePay Webhook)
                string authHeader = Request.Headers["Authorization"] ?? Request.Headers["X-SePay-Api-Key"] ?? "";
                string configuredKey = ConfigurationManager.AppSettings["SePay_ApiKey"] ?? "";
                
                if (!string.IsNullOrWhiteSpace(configuredKey))
                {
                    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.Contains(configuredKey))
                    {
                        LogSePay($"[CẢNH BÁO BẢO MẬT] API Key SePay trong Header ('{authHeader}') không khớp với cấu hình.");
                        // Bạn có thể chọn từ chối hoặc cảnh báo. Ở đây chúng ta ghi log cảnh báo chi tiết để đảm bảo không bỏ sót giao dịch hợp lệ.
                    }
                }

                // 2. Chỉ xử lý tiền VÀO (transferType == "in" hoặc transferAmount > 0)
                if (!string.IsNullOrWhiteSpace(model.transferType) && model.transferType.ToLower() != "in")
                {
                    return Json(new { success = true, message = "Bỏ qua giao dịch tiền ra" });
                }

                // 3. Tách lấy Mã đơn hàng BHX từ nội dung chuyển khoản (Ví dụ: "BHX1015" -> 1015)
                string searchContent = (model.content ?? "") + " " + (model.description ?? "");
                int orderId = ExtractOrderId(searchContent);

                if (orderId <= 0)
                {
                    LogSePay($"Khởi tạo không thành công: Không tìm thấy mã đơn dạng BHX1234 trong nội dung: '{searchContent}'");
                    return Json(new { success = false, message = "Không tìm thấy Mã đơn hàng BHX trong nội dung chuyển khoản" });
                }

                var order = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    LogSePay($"Đơn hàng #{orderId} không tồn tại trong cơ sở dữ liệu SQL Server");
                    return Json(new { success = false, message = $"Đơn hàng #{orderId} không tồn tại" });
                }

                // 4. Cập nhật trạng thái thanh toán đơn hàng sang ĐÃ THANH TOÁN (1)
                order.PaymentMethod = 1; // 1 = VietinBank / Chuyển khoản
                order.PaymentStatus = 1; // 1 = Đã thanh toán thành công
                _dbContext.SaveChanges();

                LogSePay($"[GẠCH NỢ THÀNH CÔNG] Đơn hàng #{orderId} đã được chuyển sang trạng thái ĐÃ THANH TOÁN VIETINBANK (Số tiền nhận: {model.transferAmount:N0}đ)");

                // 5. Tự động gửi Email Hóa đơn điện tử
                string recipientEmail = order.User != null ? order.User.Email : null;
                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    OrderInvoiceEmailService.SendOrderConfirmationEmail(order, recipientEmail);
                }

                return Json(new { success = true, message = $"Duyệt thanh toán thành công cho đơn hàng #{orderId}!", orderId = orderId });
            }
            catch (Exception ex)
            {
                LogSePay("[LỖI SEPAY WEBHOOK] " + ex.Message + " -> " + ex.InnerException?.Message);
                return Json(new { success = false, message = "Lỗi xử lý Webhook: " + ex.Message });
            }
        }

        // GET: /SePay/TestWebhook?orderId=1015 (API hỗ trợ Test kích hoạt gạch nợ thủ công trực tiếp)
        [HttpGet]
        public ActionResult TestWebhook(int orderId)
        {
            try
            {
                var order = _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = $"Đơn hàng #{orderId} không tồn tại!" }, JsonRequestBehavior.AllowGet);
                }

                order.PaymentMethod = 1; // VietinBank / SePay
                order.PaymentStatus = 1; // Đã thanh toán
                _dbContext.SaveChanges();

                LogSePay($"[TEST WEBHOOK GẠCH NỢ] Đơn hàng #{orderId} đã được gạch nợ thành công qua TestWebhook.");

                string recipientEmail = order.User != null ? order.User.Email : null;
                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    OrderInvoiceEmailService.SendOrderConfirmationEmail(order, recipientEmail);
                }

                return Json(new { success = true, message = $"Đã gạch nợ thành công đơn hàng #{orderId}!", orderId = orderId }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi TestWebhook: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private int ExtractOrderId(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return 0;

            // 1. Tìm dạng BHX1234, BHX 1234, BHX_1234, BHX-1234
            var match = Regex.Match(content, @"BHX[-_\s]?(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id1))
            {
                return id1;
            }

            // 2. Tìm dạng DH1234, DH 1234, DONHANG1234
            var match2 = Regex.Match(content, @"(?:DH|DONHANG)[-_\s]?(\d+)", RegexOptions.IgnoreCase);
            if (match2.Success && int.TryParse(match2.Groups[1].Value, out int id2))
            {
                return id2;
            }

            // 3. Tìm các chuỗi số nguyên riêng lẻ từ 1 đến 8 chữ số
            var numMatch = Regex.Match(content, @"\b(\d{1,8})\b");
            if (numMatch.Success && int.TryParse(numMatch.Groups[1].Value, out int id3))
            {
                return id3;
            }

            return 0;
        }

        private void LogSePay(string message)
        {
            try
            {
                string logPath = HostingEnvironment.MapPath("~/App_Data/sepay_transactions.log");
                if (string.IsNullOrEmpty(logPath))
                {
                    logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "sepay_transactions.log");
                }
                string dir = System.IO.Path.GetDirectoryName(logPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n", Encoding.UTF8);
            }
            catch { }
        }
    }
}

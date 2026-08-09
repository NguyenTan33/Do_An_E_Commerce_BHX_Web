using System;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Hosting;
using Do_An_E_Commerce_BHX.Controllers;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class SePayWebhookService : ISePayWebhookService
    {
        private readonly ApplicationDbContext _dbContext;

        public SePayWebhookService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? new ApplicationDbContext();
        }

        public async Task<(bool Success, string Message, int OrderId)> ProcessWebhookAsync(SePayWebhookModel model, string authHeader)
        {
            if (model == null)
            {
                return (false, "Payload SePay rỗng", 0);
            }

            string logContent = $"Gateway: {model.gateway}, Amount: {model.transferAmount}, Content: '{model.content}', Account: {model.accountNumber}";
            LogSePay(logContent);

            string configuredKey = ConfigurationManager.AppSettings["SePay_ApiKey"] ?? "";
            if (!string.IsNullOrWhiteSpace(configuredKey))
            {
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.Contains(configuredKey))
                {
                    LogSePay($"[CẢNH BÁO BẢO MẬT] API Key SePay trong Header ('{authHeader}') không khớp với cấu hình.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.transferType) && model.transferType.ToLower() != "in")
            {
                return (true, "Bỏ qua giao dịch tiền ra", 0);
            }

            string searchContent = (model.content ?? "") + " " + (model.description ?? "");
            int orderId = ExtractOrderId(searchContent);

            if (orderId <= 0)
            {
                LogSePay($"Khởi tạo không thành công: Không tìm thấy mã đơn dạng BHX1234 trong nội dung: '{searchContent}'");
                return (false, "Không tìm thấy Mã đơn hàng BHX trong nội dung chuyển khoản", 0);
            }

            var order = await _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                LogSePay($"Đơn hàng #{orderId} không tồn tại trong cơ sở dữ liệu SQL Server");
                return (false, $"Đơn hàng #{orderId} không tồn tại", 0);
            }

            order.PaymentMethod = 1;
            order.PaymentStatus = 1;
            await _dbContext.SaveChangesAsync();

            LogSePay($"[GẠCH NỢ THÀNH CÔNG] Đơn hàng #{orderId} đã được chuyển sang trạng thái ĐÃ THANH TOÁN VIETINBANK (Số tiền nhận: {model.transferAmount:N0}đ)");

            string recipientEmail = order.User != null ? order.User.Email : null;
            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                OrderInvoiceEmailService.SendOrderConfirmationEmail(order, recipientEmail);
            }

            return (true, $"Duyệt thanh toán thành công cho đơn hàng #{orderId}!", orderId);
        }

        public async Task<(bool Success, string Message, int OrderId)> TestWebhookAsync(int orderId)
        {
            var order = await _dbContext.Order.Include("OrderDetails.Product").Include("User").FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return (false, $"Đơn hàng #{orderId} không tồn tại!", orderId);
            }

            order.PaymentMethod = 1;
            order.PaymentStatus = 1;
            await _dbContext.SaveChangesAsync();

            LogSePay($"[TEST WEBHOOK GẠCH NỢ] Đơn hàng #{orderId} đã được gạch nợ thành công qua TestWebhook.");

            string recipientEmail = order.User != null ? order.User.Email : null;
            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                OrderInvoiceEmailService.SendOrderConfirmationEmail(order, recipientEmail);
            }

            return (true, $"Đã gạch nợ thành công đơn hàng #{orderId}!", orderId);
        }

        private int ExtractOrderId(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return 0;

            var match = Regex.Match(content, @"BHX[-_\s]?(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id1))
            {
                return id1;
            }

            var match2 = Regex.Match(content, @"(?:DH|DONHANG)[-_\s]?(\d+)", RegexOptions.IgnoreCase);
            if (match2.Success && int.TryParse(match2.Groups[1].Value, out int id2))
            {
                return id2;
            }

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

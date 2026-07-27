using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class OrderInvoiceEmailService
    {
        public static void SendOrderConfirmationEmail(Order order, string recipientEmail = null)
        {
            if (order == null) return;

            try
            {
                // CHỈ GỬI MAIL KHI KHÁCH HÀNG ĐÃ ĐĂNG NHẬP VÀ CÓ EMAIL HỢP LỆ
                string toEmail = recipientEmail;
                if (string.IsNullOrWhiteSpace(toEmail) && order.User != null)
                {
                    toEmail = !string.IsNullOrWhiteSpace(order.User.Email) ? order.User.Email : order.User.UserName;
                }

                // Nếu là khách chưa đăng nhập (hoặc email rỗng/không chứa @), KHÔNG gửi mail
                if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
                {
                    LogError($"[HUỶ GỬI MAIL] Đơn hàng #{order.Id} là khách vãng lai hoặc không có địa chỉ Email hợp lệ ({toEmail}).");
                    return;
                }

                toEmail = toEmail.Trim();
                string subject = $"[Bách Hóa Xanh] Hóa đơn điện tử đơn hàng #{order.Id}";
                string htmlBody = BuildOrderEmailTemplate(order);

                // 1. LUÔN LƯU BẢN SAO HÓA ĐƠN HTML VÀO THƯ MỤC CONTENT/INVOICES (Xem trực tiếp /Content/Invoices/Invoice_Order_ID.html)
                SaveInvoiceHtmlFile(order.Id, htmlBody);

                // 2. ĐỌC CẤU HÌNH GỬI MAIL SMTP TRONG WEB.CONFIG
                string smtpHost = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
                int smtpPort = 587;
                int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out smtpPort);
                if (smtpPort <= 0) smtpPort = 587;

                string smtpUser = ConfigurationManager.AppSettings["SmtpUser"] ?? "";
                string smtpPass = ConfigurationManager.AppSettings["SmtpPass"] ?? "";
                string fromEmail = ConfigurationManager.AppSettings["FromEmail"] ?? "";

                if (string.IsNullOrWhiteSpace(fromEmail)) fromEmail = smtpUser;

                if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
                {
                    // Cơ chế Thử lại (Retry) 3 lần để phòng chống lỗi chập chờn DNS / Mạng máy cục bộ
                    bool sentSuccess = false;
                    Exception lastException = null;

                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        try
                        {
                            using (MailMessage mail = new MailMessage())
                            {
                                mail.From = new MailAddress(fromEmail.Trim(), "Bách Hóa Xanh Online");
                                mail.To.Add(toEmail);
                                mail.Subject = subject;
                                mail.Body = htmlBody;
                                mail.IsBodyHtml = true;
                                mail.BodyEncoding = Encoding.UTF8;

                                using (SmtpClient smtp = new SmtpClient(smtpHost.Trim(), smtpPort))
                                {
                                    smtp.Credentials = new NetworkCredential(smtpUser.Trim(), smtpPass.Trim());
                                    smtp.EnableSsl = true;
                                    smtp.Timeout = 15000; // 15 giây timeout
                                    smtp.Send(mail);
                                }
                            }

                            sentSuccess = true;
                            LogError($"[SMTP GỬI THÀNH CÔNG (Lần {attempt})] Đã gửi thành công Email Hóa đơn #{order.Id} tới hòm thư {toEmail}.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            System.Threading.Thread.Sleep(1000); // Đợi 1 giây rồi thử lại
                        }
                    }

                    if (!sentSuccess && lastException != null)
                    {
                        LogError($"[LỖI GỬI MAIL SMTP THẤT BẠI 3 LẦN] Đơn hàng #{order.Id} gửi tới {toEmail} thất bại: {lastException.Message} -> {lastException.InnerException?.Message}");
                    }
                }
                else
                {
                    LogError($"[CHÚ Ý GỬI MAIL] Đã khởi tạo Hóa đơn #{order.Id} tới email {toEmail}. Tuy nhiên chưa điền SmtpUser/SmtpPass trong Web.config.");
                }
            }
            catch (Exception ex)
            {
                LogError($"[LỖI GỬI MAIL] Đơn hàng #{order.Id} gửi tới {recipientEmail} thất bại: {ex.Message} -> {ex.InnerException?.Message}");
            }
        }

        public static void SendOrderConfirmationEmailAsync(Order order, string recipientEmail = null)
        {
            HostingEnvironment.QueueBackgroundWorkItem(cancellationToken =>
            {
                SendOrderConfirmationEmail(order, recipientEmail);
            });
        }

        private static void SaveInvoiceHtmlFile(int orderId, string htmlContent)
        {
            try
            {
                string folderPath = HostingEnvironment.MapPath("~/Content/Invoices");
                if (string.IsNullOrEmpty(folderPath))
                {
                    folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Invoices");
                }

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, $"Invoice_Order_{orderId}.html");
                File.WriteAllText(filePath, htmlContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi lưu file Hóa đơn HTML: " + ex.Message);
            }
        }

        private static void LogError(string message)
        {
            try
            {
                string logPath = HostingEnvironment.MapPath("~/App_Data/email_errors.log");
                if (string.IsNullOrEmpty(logPath))
                {
                    logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "email_errors.log");
                }

                string dir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n", Encoding.UTF8);
            }
            catch { }
        }

        private static string BuildOrderEmailTemplate(Order order)
        {
            string paymentMethodText = order.PaymentMethod == 0 ? "Thanh toán khi nhận hàng (COD)" :
                                      (order.PaymentMethod == 2 ? "Ví điện tử MoMo" : "Chuyển khoản Ngân hàng (Banking)");

            string paymentStatusText = order.PaymentStatus == 1 ? "<span style='color: #2e7d32; font-weight: bold;'>Đã thanh toán thành công</span>" :
                                                                 "<span style='color: #ed6c02; font-weight: bold;'>Chờ thu tiền khi giao hàng</span>";

            StringBuilder sb = new StringBuilder();
            sb.Append(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body { font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; color: #333; }
        .container { max-width: 650px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.08); }
        .header { background: #008848; padding: 25px 30px; text-align: center; color: #ffffff; }
        .header h1 { margin: 0; font-size: 24px; text-transform: uppercase; letter-spacing: 1px; }
        .header p { margin: 5px 0 0; font-size: 14px; opacity: 0.9; }
        .content { padding: 30px; }
        .section-title { font-size: 15px; font-weight: bold; color: #008848; border-bottom: 2px solid #e8f5e9; padding-bottom: 8px; margin-bottom: 15px; text-transform: uppercase; }
        .info-table { width: 100%; border-collapse: collapse; margin-bottom: 25px; }
        .info-table td { padding: 6px 0; font-size: 14px; vertical-align: top; }
        .info-label { color: #666; width: 160px; font-weight: 500; }
        .info-value { color: #111; font-weight: 600; }
        .items-table { width: 100%; border-collapse: collapse; margin-bottom: 25px; }
        .items-table th { background: #f8f9fa; color: #555; text-align: left; padding: 10px; font-size: 13px; border-bottom: 2px solid #dee2e6; }
        .items-table td { padding: 12px 10px; font-size: 14px; border-bottom: 1px solid #eee; }
        .total-box { background: #f9fbf9; border: 1px solid #e0f2f1; border-radius: 8px; padding: 15px 20px; margin-bottom: 25px; }
        .total-row { display: flex; justify-content: space-between; padding: 5px 0; font-size: 14px; }
        .total-final { font-size: 18px; font-weight: bold; color: #d32f2f; border-top: 1px solid #ddd; padding-top: 10px; margin-top: 5px; }
        .footer { background: #f1f3f5; padding: 20px 30px; text-align: center; font-size: 13px; color: #666; border-top: 1px solid #e9ecef; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>BÁCH HÓA XANH ONLINE</h1>
            <p>Hóa đơn mua hàng điện tử dành cho quý khách</p>
        </div>
        <div class='content'>
            <div style='background: #e8f5e9; border: 1px solid #c8e6c9; border-radius: 8px; padding: 15px; text-align: center; margin-bottom: 25px;'>
                <h3 style='margin:0; color: #2e7d32;'>🧾 HÓA ĐƠN MUA HÀNG</h3>
                <p style='margin: 5px 0 0; color: #388e3c; font-size: 14px;'>Mã đơn hàng: <strong style='font-size: 16px;'>#" + order.Id + @"</strong></p>
            </div>

            <div class='section-title'>THÔNG TIN ĐƠN HÀNG</div>
            <table class='info-table'>
                <tr><td class='info-label'>Mã đơn hàng:</td><td class='info-value'>#" + order.Id + @"</td></tr>
                <tr><td class='info-label'>Ngày mua hàng:</td><td class='info-value'>" + order.OrderDate.ToString("dd/MM/yyyy HH:mm") + @"</td></tr>
                <tr><td class='info-label'>Phương thức thanh toán:</td><td class='info-value'>" + paymentMethodText + @"</td></tr>
                <tr><td class='info-label'>Trạng thái thanh toán:</td><td class='info-value'>" + paymentStatusText + @"</td></tr>
            </table>

            <div class='section-title'>THÔNG TIN KHÁCH HÀNG & GIAO HÀNG</div>
            <table class='info-table'>
                <tr><td class='info-label'>Người nhận hàng:</td><td class='info-value'>" + order.ReceiverName + @"</td></tr>
                <tr><td class='info-label'>Số điện thoại:</td><td class='info-value'>" + order.ReceiverPhone + @"</td></tr>
                <tr><td class='info-label'>Địa chỉ giao hàng:</td><td class='info-value'>" + order.ShippingAddress + @"</td></tr>
                " + (!string.IsNullOrWhiteSpace(order.Note) ? "<tr><td class='info-label'>Ghi chú:</td><td class='info-value'>" + order.Note + "</td></tr>" : "") + @"
            </table>

            <div class='section-title'>CHI TIẾT MÓN HÀNG HÓA ĐƠN</div>
            <table class='items-table'>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                        <th style='text-align: right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>");

            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                foreach (var detail in order.OrderDetails)
                {
                    string pName = detail.Product != null ? detail.Product.Name : "Sản phẩm";
                    double total = detail.Price * detail.Quantity;
                    sb.Append($@"
                    <tr>
                        <td><strong>{pName}</strong></td>
                        <td style='text-align: center;'>{detail.Quantity}</td>
                        <td style='text-align: right;'>{detail.Price:N0} ₫</td>
                        <td style='text-align: right; font-weight: bold;'>{total:N0} ₫</td>
                    </tr>");
                }
            }

            sb.Append(@"
                </tbody>
            </table>

            <div class='total-box'>
                <div class='total-row'><span>Tiền hàng:</span><strong>" + (order.TotalAmount + order.DiscountAmount + order.PointDiscountAmount - order.ShippingFee).ToString("N0") + @" ₫</strong></div>
                <div class='total-row'><span>Phí giao hàng:</span><strong>" + order.ShippingFee.ToString("N0") + @" ₫</strong></div>");

            if (order.DiscountAmount > 0)
            {
                sb.Append("<div class='total-row' style='color: #2e7d32;'><span>Mã giảm giá:</span><strong>-" + order.DiscountAmount.ToString("N0") + " ₫</strong></div>");
            }
            if (order.PointDiscountAmount > 0)
            {
                sb.Append("<div class='total-row' style='color: #2e7d32;'><span>Giảm giá từ điểm tích lũy:</span><strong>-" + order.PointDiscountAmount.ToString("N0") + " ₫</strong></div>");
            }

            sb.Append(@"
                <div class='total-row total-final'><span>TỔNG TIỀN THANH TOÁN:</span><span>" + order.TotalAmount.ToString("N0") + @" ₫</span></div>
            </div>

            <p style='font-size: 13px; color: #666; text-align: center; margin-top: 20px;'>
                Đây là email tự động gửi hóa đơn mua hàng. Quý khách không cần thao tác bấm xác nhận.
            </p>
        </div>
        <div class='footer'>
            <p style='margin: 0 0 5px; font-weight: bold; color: #008848;'>HỆ THỐNG SIÊU THỊ BÁCH HÓA XANH ONLINE</p>
            <p style='margin: 0;'>Hotline hỗ trợ: 1900 1908 - Email: cskh@bachhoaxanh.com</p>
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }
    }
}

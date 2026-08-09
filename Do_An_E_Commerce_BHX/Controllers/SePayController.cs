using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;

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

    public class SePayController : BaseController
    {
        private readonly ISePayWebhookService _sePayWebhookService;

        public SePayController()
        {
            _sePayWebhookService = new SePayWebhookService(DbContext);
        }

        public SePayController(ISePayWebhookService sePayWebhookService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _sePayWebhookService = sePayWebhookService ?? new SePayWebhookService(DbContext);
        }

        // POST: /SePay/Webhook (Nhận Webhook số dư tiền vào VietinBank từ SePay)
        [HttpPost]
        public async Task<ActionResult> Webhook(SePayWebhookModel model)
        {
            try
            {
                string authHeader = Request.Headers["Authorization"] ?? Request.Headers["X-SePay-Api-Key"] ?? "";
                var (success, message, orderId) = await _sePayWebhookService.ProcessWebhookAsync(model, authHeader);
                if (success && orderId > 0)
                {
                    return Json(new { success = true, message = message, orderId = orderId });
                }
                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xử lý Webhook: " + ex.Message });
            }
        }

        // GET: /SePay/TestWebhook?orderId=1015 (API hỗ trợ Test kích hoạt gạch nợ thủ công trực tiếp)
        [HttpGet]
        public async Task<ActionResult> TestWebhook(int orderId)
        {
            try
            {
                var (success, message, resOrderId) = await _sePayWebhookService.TestWebhookAsync(orderId);
                return Json(new { success = success, message = message, orderId = resOrderId }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi TestWebhook: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}

using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Controllers;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface ISePayWebhookService
    {
        Task<(bool Success, string Message, int OrderId)> ProcessWebhookAsync(SePayWebhookModel model, string authHeader);
        Task<(bool Success, string Message, int OrderId)> TestWebhookAsync(int orderId);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IStoreProductService
    {
        List<Product> SearchProducts(string searchName, int? categoryId, out List<Category> categories);
        Task<Product> GetProductDetailAsync(int productId);
        List<Review> GetProductReviews(int productId);
        List<Question> GetProductQuestions(int productId);

        (bool Success, string Message, object Data) GetProductDetailJsonData(int productId, bool isAdmin);
        (bool Success, string Message, object Data) GetProductUnitsJsonData(int productId);

        void LogSearchAnalytics(string searchName, string userId, string userIp, string userAgent, string sessionId, Uri referrer);
        void LogViewProductAnalytics(int productId, string productName, string userId, string userIp, string userAgent, string sessionId, Uri referrer);
    }
}

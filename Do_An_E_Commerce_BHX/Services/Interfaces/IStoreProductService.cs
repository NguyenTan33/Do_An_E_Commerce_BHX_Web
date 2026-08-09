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
    }
}

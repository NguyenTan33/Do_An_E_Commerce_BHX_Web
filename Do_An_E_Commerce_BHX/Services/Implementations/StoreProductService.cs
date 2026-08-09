using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class StoreProductService : IStoreProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly ProductService _productService;
        private readonly CustomerSupportService _customerSupportService;

        public StoreProductService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
            var searchHandler = new SearchHandler(_db);
            _productService = new ProductService(_db, searchHandler);
            _customerSupportService = new CustomerSupportService(_db);
        }

        public List<Product> SearchProducts(string searchName, int? categoryId, out List<Category> categories)
        {
            var filter = new ProductType
            {
                name = string.IsNullOrWhiteSpace(searchName) ? null : searchName,
                category = categoryId.HasValue ? _db.Category.Find(categoryId.Value) : null
            };

            var list = _productService.Search(filter);
            categories = _db.Category.ToList();
            return list;
        }

        public async Task<Product> GetProductDetailAsync(int productId)
        {
            return await _db.Product.Include(p => p.ParentProduct).FirstOrDefaultAsync(p => p.Id == productId);
        }

        public List<Review> GetProductReviews(int productId)
        {
            return _customerSupportService.GetAllReviewsByProductID(productId);
        }

        public List<Question> GetProductQuestions(int productId)
        {
            return _customerSupportService.GetAllQuestionsByProductId(productId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Controllers;
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

        public (bool Success, string Message, object Data) GetProductDetailJsonData(int productId, bool isAdmin)
        {
            var product = _db.Product.Find(productId);
            if (product == null)
            {
                return (false, "Không tìm thấy sản phẩm!", null);
            }

            var category = _db.Category.Find(product.CategoryId);
            var reviews = _db.Review.Where(r => r.ProductId == productId).ToList();
            double avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0;
            int reviewCount = reviews.Count;

            var reviewList = reviews.OrderByDescending(r => r.CreatedDate).Select(r => {
                string rName = "Khách hàng";
                if (!string.IsNullOrEmpty(r.UserId) && r.UserId != "GUEST")
                {
                    var user = _db.Users.FirstOrDefault(u => u.Id == r.UserId);
                    if (user != null)
                    {
                        rName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.UserName;
                    }
                }

                return new
                {
                    id = r.Id,
                    userName = rName,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdDate = r.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                };
            }).ToList();

            var questions = _db.Question
                .Where(q => q.ProductId == productId)
                .OrderByDescending(q => q.CreatedDate)
                .ToList();

            var questionList = questions.Select(q => {
                string senderName = "Khách hàng";
                if (q.UserId > 0)
                {
                    string uidStr = q.UserId.ToString();
                    var user = _db.Users.FirstOrDefault(u => u.Id == uidStr);
                    if (user != null)
                    {
                        senderName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.UserName;
                    }
                }

                return new
                {
                    id = q.Id,
                    userId = q.UserId.ToString(),
                    userName = senderName,
                    content = q.Content,
                    createdDate = q.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                    answer = q.Answer,
                    answerBy = "Bách Hóa Xanh"
                };
            }).ToList();

            var data = new
            {
                id = product.Id,
                name = product.Name,
                price = product.Price,
                quantity = product.Quantity,
                description = product.Description ?? "Sản phẩm tươi ngon, đảm bảo chất lượng chính hãng từ Bách Hóa Xanh.",
                imageUrl = string.IsNullOrEmpty(product.URLImage) ? "/Content/images/no-image.png" : product.URLImage,
                categoryName = category != null ? category.Name : "Nhu yếu phẩm",
                avgRating = avgRating,
                reviewCount = reviewCount,
                reviews = reviewList,
                questions = questionList
            };

            return (true, null, data);
        }

        public (bool Success, string Message, object Data) GetProductUnitsJsonData(int productId)
        {
            var product = _db.Product.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return (false, "Không tìm thấy sản phẩm", null);
            }

            var units = _db.ProductUnit
                .Where(u => u.ProductId == productId)
                .OrderByDescending(u => u.IsDefault)
                .ThenBy(u => u.ConversionFactor)
                .ToList();

            if (!units.Any())
            {
                units.Add(new ProductUnit
                {
                    ProductId = productId,
                    UnitName = !string.IsNullOrEmpty(product.Unit) ? product.Unit : "Cái",
                    Price = product.Price,
                    ConversionFactor = product.UnitMultiplier > 0 ? product.UnitMultiplier : 1,
                    IsDefault = true
                });
            }

            var resultList = units.Select(u => new
            {
                id = u.Id,
                unitName = u.UnitName,
                price = u.Price,
                priceFormatted = u.Price.ToString("N0") + " ₫",
                conversionFactor = u.ConversionFactor,
                isDefault = u.IsDefault,
                canFulfill = product.Quantity >= u.ConversionFactor,
                stockRemaining = product.Quantity
            }).ToList();

            var data = new
            {
                success = true,
                productId = product.Id,
                productName = product.Name,
                totalStock = product.Quantity,
                units = resultList
            };

            return (true, null, data);
        }

        public void LogSearchAnalytics(string searchName, string userId, string userIp, string userAgent, string sessionId, Uri referrer)
        {
            if (string.IsNullOrWhiteSpace(searchName)) return;
            try
            {
                var analyticsService = new AnalyticsService(_db);
                var dto = new AnalyticsController.BehaviorEventDto
                {
                    EventType = "SearchKeyword",
                    TargetName = searchName.Trim()
                };
                Task.Run(() => analyticsService.LogBehaviorEventAsync(dto, userId, userIp, userAgent, sessionId, referrer));
            }
            catch { }
        }

        public void LogViewProductAnalytics(int productId, string productName, string userId, string userIp, string userAgent, string sessionId, Uri referrer)
        {
            try
            {
                var analyticsService = new AnalyticsService(_db);
                var dto = new AnalyticsController.BehaviorEventDto
                {
                    EventType = "ViewProduct",
                    TargetId = productId,
                    TargetName = productName
                };
                _ = Task.Run(() => analyticsService.LogBehaviorEventAsync(dto, userId, userIp, userAgent, sessionId, referrer));
            }
            catch { }
        }
    }
}

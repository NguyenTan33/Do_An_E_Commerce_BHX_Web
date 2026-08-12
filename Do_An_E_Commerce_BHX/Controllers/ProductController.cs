using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class ProductController : BaseController
    {
        private readonly IStoreProductService _storeProductService;
        private readonly CustomerSupportService _customerSupportService;

        public ProductController()
        {
            _storeProductService = new StoreProductService(DbContext);
            _customerSupportService = new CustomerSupportService(DbContext);
        }

        public ProductController(IStoreProductService storeProductService, CustomerSupportService customerSupportService, ApplicationDbContext dbContext)
            : base(dbContext)
        {
            _storeProductService = storeProductService ?? new StoreProductService(DbContext);
            _customerSupportService = customerSupportService ?? new CustomerSupportService(DbContext);
        }

        // Trang hiển thị danh sách sản phẩm + tìm kiếm
        public ActionResult Index(string searchName, int? categoryId)
        {
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                try
                {
                    var analyticsService = new AnalyticsService(DbContext);
                    string currentUserId = User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;
                    string userIp = Request.UserHostAddress;
                    string userAgent = Request.UserAgent;
                    string sessionId = Session.SessionID;

                    var dto = new AnalyticsController.BehaviorEventDto
                    {
                        EventType = "SearchKeyword",
                        TargetName = searchName.Trim()
                    };

                    Task.Run(() => analyticsService.LogBehaviorEventAsync(dto, currentUserId, userIp, userAgent, sessionId, Request.UrlReferrer));
                }
                catch { }
            }

            var productList = _storeProductService.SearchProducts(searchName, categoryId, out var categories);

            ViewBag.Categories = categories;
            ViewBag.CurrentName = searchName;
            ViewBag.CurrentCategory = categoryId;

            return View(productList);
        }

        [HttpGet]
        public async Task<ActionResult> Detail(int productId)
        {
            Product product = await _storeProductService.GetProductDetailAsync(productId);
            if (product == null) return HttpNotFound();

            try
            {
                var analyticsService = new AnalyticsService(DbContext);
                string currentUserId = User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;
                string userIp = Request.UserHostAddress;
                string userAgent = Request.UserAgent;
                string sessionId = Session.SessionID;

                var dto = new AnalyticsController.BehaviorEventDto
                {
                    EventType = "ViewProduct",
                    TargetId = productId,
                    TargetName = product.Name
                };

                _ = Task.Run(() => analyticsService.LogBehaviorEventAsync(dto, currentUserId, userIp, userAgent, sessionId, Request.UrlReferrer));
            }
            catch { }

            ViewBag.Reviews = _storeProductService.GetProductReviews(productId);
            ViewBag.Questions = _storeProductService.GetProductQuestions(productId);

            return View(product);
        }

        // 1. Khách gửi Đánh giá (Rating + Comment)
        [HttpPost]
        public ActionResult PostReview(int productId, int rating, string comment)
        {
            try
            {
                string userId = GetCurrentUserId();
                _customerSupportService.AddReview(productId, userId, rating, comment);

                return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá sản phẩm!" });
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? (ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message) : ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // 2. Khách gửi Câu hỏi (Hỏi đáp)
        [HttpPost]
        public ActionResult PostQuestion(int productId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Vui lòng nhập nội dung bình luận!" });
                }

                string trimmedContent = content.Trim();
                if (trimmedContent.Length > 200)
                {
                    return Json(new { success = false, message = "Nội dung bình luận/câu hỏi không được vượt quá 200 ký tự!" });
                }

                string userId = GetCurrentUserId();
                _customerSupportService.AddQuestion(productId, userId, trimmedContent);

                return Json(new { success = true, message = "Đã gửi bình luận/câu hỏi thành công! QTV sẽ phản hồi sớm nhất." });
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? (ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message) : ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // 3. GET: /Product/GetProductDetailJson?productId=123
        [HttpGet]
        public ActionResult GetProductDetailJson(int productId)
        {
            var product = DbContext.Product.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" }, JsonRequestBehavior.AllowGet);
            }

            var category = DbContext.Category.Find(product.CategoryId);
            var reviews = DbContext.Review.Where(r => r.ProductId == productId).ToList();
            double avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0;
            int reviewCount = reviews.Count;

            var reviewList = reviews.OrderByDescending(r => r.CreatedDate).Select(r => {
                string rName = "Khách hàng";
                if (!string.IsNullOrEmpty(r.UserId) && r.UserId != "GUEST")
                {
                    var user = DbContext.Users.FirstOrDefault(u => u.Id == r.UserId);
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

            var questions = DbContext.Question
                .Where(q => q.ProductId == productId)
                .OrderByDescending(q => q.CreatedDate)
                .ToList();

            var questionList = questions.Select(q => {
                string senderName = "Khách hàng";
                if (q.UserId > 0)
                {
                    string uidStr = q.UserId.ToString();
                    var user = DbContext.Users.FirstOrDefault(u => u.Id == uidStr);
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

            bool isAdmin = User.IsInRole("Admin");

            return Json(new
            {
                success = true,
                isAdmin = isAdmin,
                data = new
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
                }
            }, JsonRequestBehavior.AllowGet);
        }

        // 4. POST: /Product/PostAnswerAdmin
        [HttpPost]
        public ActionResult PostAnswerAdmin(int questionId, string answer)
        {
            try
            {
                if (!User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập quyền Admin để phản hồi!" });
                }

                if (string.IsNullOrWhiteSpace(answer))
                {
                    return Json(new { success = false, message = "Vui lòng nhập nội dung câu trả lời!" });
                }

                _customerSupportService.AddAnswer(questionId, answer.Trim());
                return Json(new { success = true, message = "Đã đăng phản hồi QTV thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // 5. GET: /Product/GetProductUnitsJson?productId=123
        [AllowAnonymous]
        [HttpGet]
        public ActionResult GetProductUnitsJson(int productId)
        {
            var product = DbContext.Product.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" }, JsonRequestBehavior.AllowGet);
            }

            var units = DbContext.ProductUnit
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

            return Json(new
            {
                success = true,
                productId = product.Id,
                productName = product.Name,
                totalStock = product.Quantity,
                units = resultList
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
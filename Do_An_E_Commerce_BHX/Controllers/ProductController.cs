using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class ProductController : BaseController
    {
        private readonly ProductService _productService;
        private readonly CustomerSupportService customerSupportService;
        private readonly ApplicationDbContext _dbContext;

        public ProductController()
        {
            // 1. Khởi tạo DbContext ĐẦU TIÊN
            _dbContext = new ApplicationDbContext();

            // 2. Sau đó mới truyền _dbContext đã có giá trị vào các Service
            customerSupportService = new CustomerSupportService(_dbContext);

            var searchHandler = new SearchHandler(_dbContext);
            _productService = new ProductService(_dbContext, searchHandler);
        }

        // Trang hiển thị danh sách sản phẩm + tìm kiếm
        public ActionResult Index(string searchName, int? categoryId)
        {
            // 1. Chuẩn bị đối tượng ProductType đúng theo class backend bạn định nghĩa
            var filter = new ProductType
            {
                name = string.IsNullOrWhiteSpace(searchName) ? null : searchName,
                category = categoryId.HasValue ? _dbContext.Category.Find(categoryId.Value) : null
            };

            // 2. Gọi hàm Search duy nhất từ ProductService
            var productList = _productService.Search(filter);

            // 3. Đưa danh mục ra ViewBag để render DropdownList ở View
            ViewBag.Categories = _dbContext.Category.ToList();
            ViewBag.CurrentName = searchName;
            ViewBag.CurrentCategory = categoryId;

            return View(productList);
        }
        [HttpGet]
        public ActionResult Detail(int productId)
        {
            Product product = _dbContext.Product.Include(p => p.ParentProduct).FirstOrDefault(p => p.Id == productId);
            if (product == null) return HttpNotFound();

            // Nhét reviews và questions vào ViewBag để View xài
            ViewBag.Reviews = customerSupportService.GetAllReviewsByProductID(productId);
            ViewBag.Questions = customerSupportService.GetAllQuestionsByProductId(productId);

            // Trả thẳng nguyên object product ra file View
            return View(product);
        }

        // 1. Khách gửi Đánh giá (Rating + Comment)
        [HttpPost]
        public ActionResult PostReview(int productId, int rating, string comment)
        {
            try
            {
                string userId = GetCurrentUserId();
                customerSupportService.AddReview(productId, userId, rating, comment);

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

                string userId = GetCurrentUserId();
                customerSupportService.AddQuestion(productId, userId, content.Trim());

                return Json(new { success = true, message = "Đã gửi bình luận/câu hỏi thành công! QTV sẽ phản hồi sớm nhất." });
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? (ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message) : ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // 3. GET: /Product/GetProductDetailJson?productId=123 (Lấy JSON chi tiết sản phẩm + lượt đánh giá + hỏi đáp QTV)
        [HttpGet]
        public ActionResult GetProductDetailJson(int productId)
        {
            var product = _dbContext.Product.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" }, JsonRequestBehavior.AllowGet);
            }

            var category = _dbContext.Category.Find(product.CategoryId);
            var reviews = _dbContext.Review.Where(r => r.ProductId == productId).ToList();
            double avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0;
            int reviewCount = reviews.Count;

            var reviewList = reviews.OrderByDescending(r => r.CreatedDate).Select(r => {
                string rName = "Khách hàng";
                if (!string.IsNullOrEmpty(r.UserId) && r.UserId != "GUEST")
                {
                    var user = _dbContext.Users.FirstOrDefault(u => u.Id == r.UserId);
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

            var questions = _dbContext.Question
                .Where(q => q.ProductId == productId)
                .OrderByDescending(q => q.CreatedDate)
                .ToList();

            var questionList = questions.Select(q => {
                string senderName = "Khách hàng";
                if (q.UserId > 0)
                {
                    string uidStr = q.UserId.ToString();
                    var user = _dbContext.Users.FirstOrDefault(u => u.Id == uidStr);
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

        // 4. POST: /Product/PostAnswerAdmin (Admin trả lời bình luận / câu hỏi)
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

                customerSupportService.AddAnswer(questionId, answer.Trim());
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
            var product = _dbContext.Product
                .FirstOrDefault(p => p.Id == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" }, JsonRequestBehavior.AllowGet);
            }

            var units = _dbContext.ProductUnit
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
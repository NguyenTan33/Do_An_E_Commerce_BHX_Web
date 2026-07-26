using System;
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
            Product product = _dbContext.Product.Find(productId);
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
                // Lấy Id nếu đã đăng nhập, nếu là Guest thì userId = null
                string userId = GetCurrentUserId();

                customerSupportService.AddReview(productId, userId, rating, comment);

                return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá sản phẩm!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // 2. Khách gửi Câu hỏi (Hỏi đáp)
        [HttpPost]
        public ActionResult PostQuestion(int productId, string content)
        {
            try
            {
                string userId = GetCurrentUserId() ;

                customerSupportService.AddQuestion(productId, userId, content);

                return Json(new { success = true, message = "Đã gửi câu hỏi! Admin sẽ trả lời sớm nhất." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
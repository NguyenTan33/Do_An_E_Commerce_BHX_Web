using System;
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
            _storeProductService.LogSearchAnalytics(searchName,
                User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null,
                Request.UserHostAddress, Request.UserAgent, Session.SessionID, Request.UrlReferrer);

            var productList = _storeProductService.SearchProducts(searchName, categoryId, out var categories);

            ViewBag.Categories = categories;
            ViewBag.CurrentName = searchName;
            ViewBag.CurrentCategory = categoryId;

            return View(productList);
        }

        // GET: /Product/Detail?productId=123
        [HttpGet]
        public async Task<ActionResult> Detail(int productId)
        {
            Product product = await _storeProductService.GetProductDetailAsync(productId);
            if (product == null) return HttpNotFound();

            _storeProductService.LogViewProductAnalytics(productId, product.Name,
                User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null,
                Request.UserHostAddress, Request.UserAgent, Session.SessionID, Request.UrlReferrer);

            ViewBag.Reviews = _storeProductService.GetProductReviews(productId);
            ViewBag.Questions = _storeProductService.GetProductQuestions(productId);

            return View(product);
        }

        // POST: /Product/PostReview (Khách gửi Đánh giá)
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

        // POST: /Product/PostQuestion (Khách gửi Câu hỏi)
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

        // GET: /Product/GetProductDetailJson?productId=123
        [HttpGet]
        public ActionResult GetProductDetailJson(int productId)
        {
            bool isAdmin = User.IsInRole("Admin");
            var (success, message, data) = _storeProductService.GetProductDetailJsonData(productId, isAdmin);

            if (!success)
            {
                return Json(new { success = false, message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, isAdmin, data }, JsonRequestBehavior.AllowGet);
        }

        // POST: /Product/PostAnswerAdmin
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

        // GET: /Product/GetProductUnitsJson?productId=123
        [AllowAnonymous]
        [HttpGet]
        public ActionResult GetProductUnitsJson(int productId)
        {
            var (success, message, data) = _storeProductService.GetProductUnitsJsonData(productId);
            if (!success)
            {
                return Json(new { success = false, message }, JsonRequestBehavior.AllowGet);
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}
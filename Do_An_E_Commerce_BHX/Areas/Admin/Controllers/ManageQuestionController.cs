using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageQuestionController : Controller
    {
        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private readonly CustomerSupportService _customerSupportService;

        public ManageQuestionController()
        {
            _customerSupportService = new CustomerSupportService(_dbContext);
        }

        // 1. GET: /Admin/ManageQuestion (Danh sách bình luận & hỏi đáp sản phẩm)
        public ActionResult Index(string filter = "unanswered", string search = "")
        {
            var query = _dbContext.Question.AsQueryable();

            // Lọc theo trạng thái phản hồi
            if (filter == "unanswered")
            {
                query = query.Where(q => q.Answer == null || q.Answer.Trim() == "");
            }
            else if (filter == "answered")
            {
                query = query.Where(q => q.Answer != null && q.Answer.Trim() != "");
            }

            // Tìm kiếm nội dung câu hỏi
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(q => q.Content.Contains(s));
            }

            var listQuestions = query.OrderByDescending(q => q.CreatedDate).ToList();

            // Nạp thông tin Tên Sản Phẩm và Tên Khách Hàng
            var productIds = listQuestions.Select(q => q.ProductId).Distinct().ToList();
            var products = _dbContext.Product.Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p);

            var userIds = listQuestions.Where(q => q.UserId > 0).Select(q => q.UserId.ToString()).Distinct().ToList();
            var users = _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName);

            ViewBag.Products = products;
            ViewBag.Users = users;

            // Đếm số lượng để làm tab counter
            ViewBag.CountUnanswered = _dbContext.Question.Count(q => q.Answer == null || q.Answer.Trim() == "");
            ViewBag.CountAnswered = _dbContext.Question.Count(q => q.Answer != null && q.Answer.Trim() != "");
            ViewBag.CountAll = _dbContext.Question.Count();

            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentSearch = search;

            return View(listQuestions);
        }

        // 2. POST: /Admin/ManageQuestion/Reply (Admin trả lời QTV ngay tại trang)
        [HttpPost]
        public ActionResult Reply(int questionId, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung câu trả lời QTV!" });
            }

            var question = _dbContext.Question.FirstOrDefault(q => q.Id == questionId);
            if (question == null)
            {
                return Json(new { success = false, message = "Không tìm thấy câu hỏi!" });
            }

            question.Answer = answer.Trim();
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Đã gửi phản hồi QTV thành công!" });
        }

        // 3. POST: /Admin/ManageQuestion/Delete (Xóa bình luận / câu hỏi của khách)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var question = _dbContext.Question.FirstOrDefault(q => q.Id == id);
            if (question == null)
            {
                return Json(new { success = false, message = "Không tìm thấy câu hỏi / bình luận này!" });
            }

            _dbContext.Question.Remove(question);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = $"Đã xóa câu hỏi / bình luận #{id} thành công!" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

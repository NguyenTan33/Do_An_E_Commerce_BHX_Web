using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManageQuestionController : AdminBaseController
    {
        private readonly IAdminQuestionService _questionService;

        public ManageQuestionController()
        {
            _questionService = new AdminQuestionService(DbContext);
        }

        public ManageQuestionController(IAdminQuestionService questionService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _questionService = questionService ?? new AdminQuestionService(DbContext);
        }

        // 1. GET: /Admin/ManageQuestion (Danh sách bình luận & hỏi đáp sản phẩm)
        public async Task<ActionResult> Index(string filter = "unanswered", string search = "")
        {
            await SetAdminFullNameViewBagAsync();

            var (listQuestions, products, users, countUnanswered, countAnswered, countAll) =
                await _questionService.GetQuestionListAsync(filter, search);

            ViewBag.Products = products;
            ViewBag.Users = users;

            ViewBag.CountUnanswered = countUnanswered;
            ViewBag.CountAnswered = countAnswered;
            ViewBag.CountAll = countAll;

            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentSearch = search;

            return View(listQuestions);
        }

        // 2. POST: /Admin/ManageQuestion/Reply (Admin trả lời QTV ngay tại trang)
        [HttpPost]
        public async Task<ActionResult> Reply(int questionId, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung câu trả lời QTV!" });
            }

            bool success = await _questionService.ReplyQuestionAsync(questionId, answer);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy câu hỏi!" });
            }

            return Json(new { success = true, message = "Đã gửi phản hồi QTV thành công!" });
        }

        // 3. POST: /Admin/ManageQuestion/Delete (Xóa bình luận / câu hỏi của khách)
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            bool success = await _questionService.DeleteQuestionAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy câu hỏi / bình luận này!" });
            }

            return Json(new { success = true, message = $"Đã xóa câu hỏi / bình luận #{id} thành công!" });
        }
    }
}

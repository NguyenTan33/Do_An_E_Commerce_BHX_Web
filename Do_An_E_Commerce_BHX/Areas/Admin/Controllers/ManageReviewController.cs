using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManageReviewController : AdminBaseController
    {
        private readonly IAdminReviewService _reviewService;

        public ManageReviewController()
        {
            _reviewService = new AdminReviewService(DbContext);
        }

        public ManageReviewController(IAdminReviewService reviewService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _reviewService = reviewService ?? new AdminReviewService(DbContext);
        }

        // 1. GET: /Admin/ManageReview (Danh sách đánh giá số sao của khách hàng)
        public async Task<ActionResult> Index(int? star = null, string search = "")
        {
            await SetAdminFullNameViewBagAsync();

            var (listReviews, products, users, countAll, count5Star, count4Star, count3Star, count2Star, count1Star) =
                await _reviewService.GetReviewsListAsync(star, search);

            ViewBag.Products = products;
            ViewBag.Users = users;

            ViewBag.CountAll = countAll;
            ViewBag.Count5Star = count5Star;
            ViewBag.Count4Star = count4Star;
            ViewBag.Count3Star = count3Star;
            ViewBag.Count2Star = count2Star;
            ViewBag.Count1Star = count1Star;

            ViewBag.CurrentStar = star;
            ViewBag.CurrentSearch = search;

            return View(listReviews);
        }

        // 2. POST: /Admin/ManageReview/Delete (Xóa bài đánh giá của khách hàng)
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            bool success = await _reviewService.DeleteReviewAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "Không tìm thấy bài đánh giá này!" });
            }

            return Json(new { success = true, message = $"Đã xóa bài đánh giá #{id} thành công!" });
        }
    }
}
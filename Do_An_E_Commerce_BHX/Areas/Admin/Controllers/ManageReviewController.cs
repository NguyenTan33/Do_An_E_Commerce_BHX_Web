using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageReviewController : Controller
    {
        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();

        // 1. GET: /Admin/ManageReview (Danh sách đánh giá số sao của khách hàng)
        public ActionResult Index(int? star = null, string search = "")
        {
            var query = _dbContext.Review.AsQueryable();

            // Lọc theo số sao (1 đến 5 sao)
            if (star.HasValue && star.Value >= 1 && star.Value <= 5)
            {
                query = query.Where(r => r.Rating == star.Value);
            }

            // Tìm kiếm theo nội dung nhận xét đánh giá
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(r => r.Comment.Contains(s));
            }

            var listReviews = query.OrderByDescending(r => r.CreatedDate).ToList();

            // Nạp thông tin Tên Sản Phẩm và Tên Khách Hàng
            var productIds = listReviews.Select(r => r.ProductId).Distinct().ToList();
            var products = _dbContext.Product.Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p);

            var userIds = listReviews.Where(r => !string.IsNullOrEmpty(r.UserId) && r.UserId != "GUEST").Select(r => r.UserId).Distinct().ToList();
            var users = _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName);

            ViewBag.Products = products;
            ViewBag.Users = users;

            // Đếm số lượng theo số sao để làm Badge Counter trên Tab
            ViewBag.CountAll = _dbContext.Review.Count();
            ViewBag.Count5Star = _dbContext.Review.Count(r => r.Rating == 5);
            ViewBag.Count4Star = _dbContext.Review.Count(r => r.Rating == 4);
            ViewBag.Count3Star = _dbContext.Review.Count(r => r.Rating == 3);
            ViewBag.Count2Star = _dbContext.Review.Count(r => r.Rating == 2);
            ViewBag.Count1Star = _dbContext.Review.Count(r => r.Rating == 1);

            ViewBag.CurrentStar = star;
            ViewBag.CurrentSearch = search;

            return View(listReviews);
        }

        // 2. POST: /Admin/ManageReview/Delete (Xóa bài đánh giá của khách hàng)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var review = _dbContext.Review.FirstOrDefault(r => r.Id == id);
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài đánh giá này!" });
            }

            _dbContext.Review.Remove(review);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = $"Đã xóa bài đánh giá #{id} thành công!" });
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
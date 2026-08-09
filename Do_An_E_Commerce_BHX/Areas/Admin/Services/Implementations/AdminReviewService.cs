using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminReviewService : IAdminReviewService
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminReviewService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? new ApplicationDbContext();
        }

        public async Task<(List<Review> Reviews, Dictionary<int, Product> Products, Dictionary<string, string> Users,
            int CountAll, int Count5Star, int Count4Star, int Count3Star, int Count2Star, int Count1Star)> GetReviewsListAsync(int? star, string search)
        {
            var query = _dbContext.Review.AsQueryable();

            if (star.HasValue && star.Value >= 1 && star.Value <= 5)
            {
                query = query.Where(r => r.Rating == star.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(r => r.Comment.Contains(s));
            }

            var listReviews = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();

            var productIds = listReviews.Select(r => r.ProductId).Distinct().ToList();
            var products = await _dbContext.Product.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

            var userIds = listReviews.Where(r => !string.IsNullOrEmpty(r.UserId) && r.UserId != "GUEST").Select(r => r.UserId).Distinct().ToList();
            var usersList = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            var users = usersList.ToDictionary(u => u.Id, u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName);

            int countAll = await _dbContext.Review.CountAsync();
            int count5Star = await _dbContext.Review.CountAsync(r => r.Rating == 5);
            int count4Star = await _dbContext.Review.CountAsync(r => r.Rating == 4);
            int count3Star = await _dbContext.Review.CountAsync(r => r.Rating == 3);
            int count2Star = await _dbContext.Review.CountAsync(r => r.Rating == 2);
            int count1Star = await _dbContext.Review.CountAsync(r => r.Rating == 1);

            return (listReviews, products, users, countAll, count5Star, count4Star, count3Star, count2Star, count1Star);
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            var review = await _dbContext.Review.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return false;

            _dbContext.Review.Remove(review);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}

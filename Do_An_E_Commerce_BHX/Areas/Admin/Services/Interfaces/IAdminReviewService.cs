using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminReviewService
    {
        Task<(List<Review> Reviews, Dictionary<int, Product> Products, Dictionary<string, string> Users,
            int CountAll, int Count5Star, int Count4Star, int Count3Star, int Count2Star, int Count1Star)> GetReviewsListAsync(int? star, string search);
        Task<bool> DeleteReviewAsync(int id);
    }
}

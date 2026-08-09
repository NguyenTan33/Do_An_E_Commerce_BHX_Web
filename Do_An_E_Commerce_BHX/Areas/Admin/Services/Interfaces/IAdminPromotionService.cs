using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminPromotionService
    {
        void SeedSampleVouchersIfEmpty();
        Task<List<Promotion>> GetFilteredPromotionsAsync(string tuKhoa, string trangThai);
        Task<Promotion> GetPromotionByIdAsync(int id);
        Task<List<Category>> GetCategoriesListAsync();
        Task<bool> IsCodeExistsAsync(string code, int? excludeId = null);
        Task<bool> CreatePromotionAsync(Promotion model, string discountType);
        Task<bool> UpdatePromotionAsync(Promotion model, string discountType);
        Task<bool> DeletePromotionAsync(int id);
    }
}

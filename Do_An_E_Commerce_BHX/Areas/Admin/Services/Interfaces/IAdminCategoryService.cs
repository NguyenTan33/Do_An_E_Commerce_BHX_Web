using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminCategoryService
    {
        Task<List<Category>> GetFilteredCategoriesAsync(string tuKhoa, string sortBy);
        Task<Category> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<(bool Success, string ErrorMessage)> DeleteCategoryAsync(int id);
    }
}

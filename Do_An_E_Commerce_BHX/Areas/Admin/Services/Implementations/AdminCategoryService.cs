using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminCategoryService : IAdminCategoryService
    {
        private readonly ApplicationDbContext _db;

        public AdminCategoryService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
        }

        public async Task<List<Category>> GetFilteredCategoriesAsync(string tuKhoa, string sortBy)
        {
            var ds = _db.Category.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                ds = ds.Where(x => x.Name.Contains(tuKhoa));
            }

            switch (sortBy)
            {
                case "nameAsc":
                    ds = ds.OrderBy(x => x.Name);
                    break;
                case "nameDesc":
                    ds = ds.OrderByDescending(x => x.Name);
                    break;
                default:
                    ds = ds.OrderBy(x => x.Id);
                    break;
            }

            return await ds.ToListAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await _db.Category.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            _db.Category.Add(category);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            var existing = await _db.Category.FirstOrDefaultAsync(x => x.Id == category.Id);
            if (existing != null)
            {
                existing.Name = category.Name;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteCategoryAsync(int id)
        {
            var category = await _db.Category.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return (false, "Không tìm thấy danh mục!");
            }

            var hasProducts = await _db.Product.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return (false, "Không thể xóa danh mục này vì đang chứa sản phẩm!");
            }

            _db.Category.Remove(category);
            await _db.SaveChangesAsync();
            return (true, null);
        }
    }
}

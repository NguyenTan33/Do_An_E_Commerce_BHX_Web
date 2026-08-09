using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminProductService
    {
        void EnsureProductUnitColumnsExist();

        Task<List<Product>> GetFilteredProductsAsync(
            string tuKhoa, int? categoryId, decimal? giaTu, decimal? giaDen,
            int? tonTu, int? tonDen, bool? isAvailable, bool? isHot,
            bool? isBestSeller, bool? isLock, int? productType, string sortBy);

        Task<Product> GetProductByIdAsync(int id);
        Task<Product> GetProductWithCategoryByIdAsync(int id);
        Task<Product> GetProductWithUnitsByIdAsync(int id);

        Task<List<Category>> GetCategoriesListAsync();
        Task<List<Product>> GetBaseProductsListAsync();

        Task<bool> CreateProductAsync(Product product, string extraUnitsJson);
        Task<bool> UpdateProductAsync(Product product, string extraUnitsJson);
        Task<bool> DeleteProductAsync(int id);
    }
}

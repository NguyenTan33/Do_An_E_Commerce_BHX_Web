using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Models.ViewModels;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IHomeService
    {
        HomeIndexViewModel GetHomeIndexData(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortBy, int page);
        List<Category> GetSidebarCategories();
    }
}

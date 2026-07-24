using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly ApplicationDbContext _dbContext;

        public ProductController()
        {
            _dbContext = new ApplicationDbContext();
            var searchHandler = new SearchHandler(_dbContext);
            _productService = new ProductService(_dbContext, searchHandler);
        }

        // Trang hiển thị danh sách sản phẩm + tìm kiếm
        public ActionResult Index(string searchName, int? categoryId)
        {
            // 1. Chuẩn bị đối tượng ProductType đúng theo class backend bạn định nghĩa
            var filter = new ProductType
            {
                name = string.IsNullOrWhiteSpace(searchName) ? null : searchName,
                category = categoryId.HasValue ? _dbContext.Category.Find(categoryId.Value) : null
            };

            // 2. Gọi hàm Search duy nhất từ ProductService
            var productList = _productService.Search(filter);

            // 3. Đưa danh mục ra ViewBag để render DropdownList ở View
            ViewBag.Categories = _dbContext.Category.ToList();
            ViewBag.CurrentName = searchName;
            ViewBag.CurrentCategory = categoryId;

            return View(productList);
        }
    }
}
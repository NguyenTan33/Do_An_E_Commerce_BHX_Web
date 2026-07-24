using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Models.ViewModels;
using Do_An_E_Commerce_BHX.Models;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // Danh sách khung giờ vàng trong ngày
        private static readonly List<FlashSaleSlot> FlashSlots = new List<FlashSaleSlot>
        {
            new FlashSaleSlot { Label = "Khung sáng",  Start = new TimeSpan(9, 0, 0),  End = new TimeSpan(11, 0, 0) },
            new FlashSaleSlot { Label = "Khung trưa",  Start = new TimeSpan(12, 0, 0), End = new TimeSpan(14, 0, 0) },
            new FlashSaleSlot { Label = "Khung tối",   Start = new TimeSpan(18, 0, 0), End = new TimeSpan(20, 0, 0) },
            new FlashSaleSlot { Label = "Khung khuya", Start = new TimeSpan(20, 0, 0), End = new TimeSpan(22, 0, 0) }
        };

        public ActionResult Index(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortBy, int page = 1)
        {
            const int pageSize = 12;

            // 1. Lấy dữ liệu cơ bản
            var query = db.Product.Where(p => p.IsAvailable && !p.IsLock);

            // 2. Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // 3. Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.Barcode.Contains(term));
            }

            // 4. Lọc theo khoảng giá (Đã bỏ ép kiểu double, dùng trực tiếp decimal)
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var totalItems = query.Count();

            // 5. Sắp xếp
            IOrderedQueryable<Product> orderedQuery;
            switch (sortBy)
            {
                case "price_asc":
                    orderedQuery = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    orderedQuery = query.OrderByDescending(p => p.Price);
                    break;
                case "name_asc":
                    orderedQuery = query.OrderBy(p => p.Name);
                    break;
                default:
                    orderedQuery = query.OrderByDescending(p => p.CreatedDate);
                    break;
            }

            // 6. Phân trang
            var products = orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var hotProducts = db.Product
                .Where(p => p.IsAvailable && !p.IsLock && p.IsHot)
                .OrderByDescending(p => p.CreatedDate)
                .Take(10)
                .ToList();

            var bestSellerProducts = db.Product
                .Where(p => p.IsAvailable && !p.IsLock && p.IsBestSeller)
                .OrderByDescending(p => p.CreatedDate)
                .Take(10)
                .ToList();

            var categories = db.Category.OrderBy(c => c.Name).ToList();

            // 7. Xử lý khung giờ vàng
            var now = DateTime.Now.TimeOfDay;
            var currentSlot = FlashSlots.FirstOrDefault(s => now >= s.Start && now < s.End);
            FlashSaleSlot nextSlot = null;

            if (currentSlot == null)
            {
                nextSlot = FlashSlots.Where(s => s.Start > now).OrderBy(s => s.Start).FirstOrDefault()
                           ?? FlashSlots.OrderBy(s => s.Start).FirstOrDefault();
            }

            // 8. Đổ vào ViewModel (Không gán TotalPages và IsSlotActive vì Model đã tự tính)
            var model = new HomeIndexViewModel
            {
                Categories = categories,
                HotProducts = hotProducts,
                BestSellerProducts = bestSellerProducts,
                Products = products,

                SearchTerm = searchTerm,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,

                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,

                AllSlots = FlashSlots,
                CurrentSlot = currentSlot,
                NextSlot = nextSlot
            };

            return View(model);
        }
        [ChildActionOnly]
        public ActionResult SidebarCategories()
        {
            // Lấy danh sách tất cả các danh mục từ Database, sắp xếp theo tên A-Z
            var categories = db.Category.OrderBy(c => c.Name).ToList();

            // Trả về PartialView và truyền data sang view _SidebarCategories.cshtml
            return PartialView("_SidebarCategories", categories);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
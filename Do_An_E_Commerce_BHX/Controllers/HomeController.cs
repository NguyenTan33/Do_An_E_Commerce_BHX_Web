using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Models.ViewModels;

// TODO: đổi "Do_An_E_Commerce_BHX.Models.ApplicationDbContext" bên dưới
// thành đúng tên/namespace DbContext hiện có trong project của bạn
// (ví dụ: BHXDbContext, Do_An_E_Commerce_BHXContext, ...).
using Do_An_E_Commerce_BHX.Models;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // Danh sách khung giờ vàng trong ngày - chỉnh sửa/thêm khung tuỳ chiến dịch
        private static readonly List<FlashSaleSlot> FlashSlots = new List<FlashSaleSlot>
        {
            new FlashSaleSlot { Label = "Khung sáng",  Start = new TimeSpan(9, 0, 0),  End = new TimeSpan(11, 0, 0) },
            new FlashSaleSlot { Label = "Khung trưa",  Start = new TimeSpan(12, 0, 0), End = new TimeSpan(14, 0, 0) },
            new FlashSaleSlot { Label = "Khung tối",   Start = new TimeSpan(18, 0, 0), End = new TimeSpan(20, 0, 0) },
            new FlashSaleSlot { Label = "Khung khuya", Start = new TimeSpan(20, 0, 0), End = new TimeSpan(22, 0, 0) },
        };

        public ActionResult Index(string searchTerm, int? categoryId, int page = 1)
        {
            const int pageSize = 12;

            // Chỉ lấy sản phẩm còn "kinh doanh" (IsAvailable) và không bị khoá (IsLock).
            // KHÔNG lọc theo Quantity ở đây -> sản phẩm hết hàng (Quantity = 0) vẫn hiện,
            // chỉ được đánh dấu "Hết hàng" ở phía View.
            var baseQuery = db.Product
                .Where(p => p.IsAvailable && !p.IsLock);

            if (categoryId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                baseQuery = baseQuery.Where(p => p.Name.Contains(term) || p.Barcode.Contains(term));
            }

            var totalItems = baseQuery.Count();

            var products = baseQuery
                .OrderByDescending(p => p.CreatedDate)
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

            // ===== Tính khung giờ vàng hiện tại / kế tiếp =====
            var now = DateTime.Now.TimeOfDay;
            var currentSlot = FlashSlots.FirstOrDefault(s => now >= s.Start && now < s.End);
            FlashSaleSlot nextSlot = null;

            if (currentSlot == null)
            {
                nextSlot = FlashSlots.Where(s => s.Start > now).OrderBy(s => s.Start).FirstOrDefault()
                           ?? FlashSlots.OrderBy(s => s.Start).FirstOrDefault(); // không còn khung nào hôm nay -> lấy khung đầu tiên ngày mai
            }

            var model = new HomeIndexViewModel
            {
                Categories = categories,
                HotProducts = hotProducts,
                BestSellerProducts = bestSellerProducts,
                Products = products,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                AllSlots = FlashSlots,
                CurrentSlot = currentSlot,
                NextSlot = nextSlot
            };

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

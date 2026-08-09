using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Models.ViewModels;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private readonly ApplicationDbContext _db;

        private static readonly List<FlashSaleSlot> FlashSlots = new List<FlashSaleSlot>
        {
            new FlashSaleSlot { Label = "Khung sáng",  Start = new TimeSpan(9, 0, 0),  End = new TimeSpan(11, 0, 0) },
            new FlashSaleSlot { Label = "Khung trưa",  Start = new TimeSpan(12, 0, 0), End = new TimeSpan(14, 0, 0) },
            new FlashSaleSlot { Label = "Khung tối",   Start = new TimeSpan(18, 0, 0), End = new TimeSpan(20, 0, 0) },
            new FlashSaleSlot { Label = "Khung khuya", Start = new TimeSpan(20, 0, 0), End = new TimeSpan(22, 0, 0) }
        };

        public HomeService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
        }

        public HomeIndexViewModel GetHomeIndexData(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortBy, int page)
        {
            ApplicationDbContext.EnsureProductColumnsExist(_db);

            const int pageSize = 12;

            var query = _db.Product.Include(p => p.ParentProduct).AsNoTracking().Where(p => p.IsAvailable && !p.IsLock);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p => p.Name.Contains(term) || (p.Barcode != null && p.Barcode.Contains(term)));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var totalItems = query.Count();

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

            var products = orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var hotProducts = _db.Product
                .Include(p => p.ParentProduct)
                .AsNoTracking()
                .Where(p => p.IsAvailable && !p.IsLock && p.IsHot)
                .OrderByDescending(p => p.CreatedDate)
                .Take(10)
                .ToList();

            var bestSellerProducts = _db.Product
                .Include(p => p.ParentProduct)
                .AsNoTracking()
                .Where(p => p.IsAvailable && !p.IsLock && p.IsBestSeller)
                .OrderByDescending(p => p.CreatedDate)
                .Take(10)
                .ToList();

            var categories = _db.Category.AsNoTracking().OrderBy(c => c.Name).ToList();

            var now = DateTime.Now.TimeOfDay;
            var currentSlot = FlashSlots.FirstOrDefault(s => now >= s.Start && now < s.End);
            FlashSaleSlot nextSlot = null;

            if (currentSlot == null)
            {
                nextSlot = FlashSlots.Where(s => s.Start > now).OrderBy(s => s.Start).FirstOrDefault()
                           ?? FlashSlots.OrderBy(s => s.Start).FirstOrDefault();
            }

            return new HomeIndexViewModel
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
        }

        public List<Category> GetSidebarCategories()
        {
            return _db.Category.AsNoTracking().OrderBy(c => c.Name).ToList();
        }
    }
}

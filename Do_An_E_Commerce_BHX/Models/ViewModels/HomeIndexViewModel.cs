using System;
using System.Collections.Generic;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Models.ViewModels
{
    // Một khung giờ vàng (flash sale theo khung giờ)
    public class FlashSaleSlot
    {
        public string Label { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }

    public class HomeIndexViewModel
    {
        // Danh mục để lọc
        public List<Category> Categories { get; set; } = new List<Category>();

        // Sản phẩm HOT (IsHot = true) - vẫn hiển thị dù Quantity = 0, miễn còn IsAvailable
        public List<Product> HotProducts { get; set; } = new List<Product>();

        // Sản phẩm bán chạy (IsBestSeller = true) - tương tự, vẫn hiện dù hết hàng
        public List<Product> BestSellerProducts { get; set; } = new List<Product>();

        // Danh sách sản phẩm chính (đã áp bộ lọc + phân trang)
        public List<Product> Products { get; set; } = new List<Product>();

        // ===== Trạng thái bộ lọc / tìm kiếm =====
        public string SearchTerm { get; set; }
        public int? CategoryId { get; set; }

        // ===== Phân trang =====
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);

        // ===== Khung giờ vàng (flash sale theo khung giờ) =====
        public List<FlashSaleSlot> AllSlots { get; set; } = new List<FlashSaleSlot>();

        // Khung đang diễn ra ngay lúc này (null nếu hiện không có khung nào đang chạy)
        public FlashSaleSlot CurrentSlot { get; set; }

        // Khung giờ vàng kế tiếp (dùng khi hiện không có khung nào đang chạy)
        public FlashSaleSlot NextSlot { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; }

        public bool IsSlotActive => CurrentSlot != null;
    }
}

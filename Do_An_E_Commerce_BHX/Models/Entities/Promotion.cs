using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; }

        public decimal DiscountValue { get; set; }
        public decimal percentDiscount { get; set; }

        // --- ĐIỀU KIỆN SHOPEE-STYLE MỚI ---
        public double MinOrderAmount { get; set; } = 0; // Giá trị đơn hàng tối thiểu (0đ, 50.000đ,...)

        public int? CategoryId { get; set; } // Null = Tất cả danh mục; N = Danh mục cụ thể (Dầu ăn,...)

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public double MaxDiscountAmount { get; set; } = 0; // Giảm tối đa (đối với giảm %)

        [StringLength(255)]
        public string Description { get; set; } // Mô tả ngắn (VD: "Đơn từ 50k - Giảm 15% tối đa 30k dành riêng Dầu ăn")

        public DateTime DateCreated { get; private set; } = DateTime.Now;
        public DateTime EffectiveDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);
        public bool IsActive { get; set; } = true;
    }
}
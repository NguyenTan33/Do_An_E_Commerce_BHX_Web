using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Barcode { get; set; }

        [Required, StringLength(250)]
        public string Name { get; set; }

        [Required, Range(0, 10000000)]
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        [StringLength(50)]
        public string Unit { get; set; } = "Cái";

        public int UnitMultiplier { get; set; } = 1;

        public int? ParentProductId { get; set; }

        [ForeignKey("ParentProductId")]
        public virtual Product ParentProduct { get; set; }

        [StringLength(100)]
        public string PackagingTag { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [StringLength(250)]
        public string URLImage { get; set; }

        [Required]
        public bool IsAvailable { get; set; }

        [Required]
        public bool IsHot { get; set; }

        [Required]
        public bool IsBestSeller { get; set; }

        [Required]
        public bool IsLock { get; set; }

        public DateTime CreatedDate { get; private set; } = DateTime.Now;

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<ProductUnit> ProductUnits { get; set; } = new List<ProductUnit>();

        /// <summary>
        /// Tồn kho khả dụng thực tế (Nếu là sản phẩm con dạng Thùng/Hộp thì tính số Thùng quy đổi từ số lượng sản phẩm mẹ)
        /// </summary>
        [NotMapped]
        public int AvailableStock
        {
            get
            {
                if (ParentProductId.HasValue && ParentProduct != null)
                {
                    int multiplier = UnitMultiplier > 0 ? UnitMultiplier : 1;
                    return ParentProduct.Quantity / multiplier;
                }
                return Quantity;
            }
        }

        /// <summary>
        /// Kiểm tra sản phẩm có hết hàng thực sự không (Nếu là sản phẩm con dạng Thùng/Hộp thì hết hàng khi tồn kho sản phẩm mẹ không đủ 1 Thùng)
        /// </summary>
        [NotMapped]
        public bool IsOutOfStock
        {
            get
            {
                if (ParentProductId.HasValue && ParentProduct != null)
                {
                    int multiplier = UnitMultiplier > 0 ? UnitMultiplier : 1;
                    return ParentProduct.Quantity < multiplier;
                }
                return Quantity <= 0;
            }
        }
    }
}
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
        public int Id { get; private set; }

        [Required, StringLength(20)]
        public string Barcode { get; set; }

        [Required, StringLength(250)]
        public string Name { get; set; }

        [Required, Range(0, 10000000)]
        public double Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [StringLength(50)]
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
    }
}
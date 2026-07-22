using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public DateTime DateCreated { get; private set; } = DateTime.Now;
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = false;
    }
}
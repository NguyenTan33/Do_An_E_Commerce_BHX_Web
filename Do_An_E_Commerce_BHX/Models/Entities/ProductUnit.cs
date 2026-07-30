using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    [Table("ProductUnit")]
    public class ProductUnit
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required, StringLength(100)]
        public string UnitName { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int ConversionFactor { get; set; } = 1;

        public bool IsDefault { get; set; } = false;
    }
}

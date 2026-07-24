using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class Category
    {
        [Key]
        public int Id { get;set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
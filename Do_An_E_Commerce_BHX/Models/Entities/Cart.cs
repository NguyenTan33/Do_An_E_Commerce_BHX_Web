using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<CartDetail> CartDetails { get; set; }
    }
}
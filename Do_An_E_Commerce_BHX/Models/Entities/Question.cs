using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class Question
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } // Khách h?i
        public string Answer { get; set; } // Admin dáp
        public DateTime CreatedDate { get; set; }
    }
}
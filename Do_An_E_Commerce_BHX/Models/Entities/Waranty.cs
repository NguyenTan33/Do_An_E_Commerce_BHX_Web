using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class Waranty
    {
        [Key]
        public int Id { get; set; }

        public int OrderDetailId { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }


        public int Status { get; set; }
    }
}
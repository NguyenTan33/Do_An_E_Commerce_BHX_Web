using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    [Table("UserPromotion")]
    public class UserPromotion
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(128)]
        public string UserId { get; set; }

        public int PromotionId { get; set; }

        [ForeignKey("PromotionId")]
        public virtual Promotion Promotion { get; set; }

        public bool IsUsed { get; set; } = false;
        public DateTime SavedDate { get; set; } = DateTime.Now;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    [Table("UserWallet")]
    public class UserWallet
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(128)]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Balance { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    [Table("UserBehaviorLog")]
    public class UserBehaviorLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(100)]
        public string SessionId { get; set; }

        [StringLength(128)]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } // ViewProduct, SearchKeyword, AddToCart, RemoveFromCart, ClickBanner, CheckoutStep, PageDwellTime, ScrollDepth, RageClick

        public int? TargetId { get; set; }

        [StringLength(255)]
        public string TargetName { get; set; }

        public int? DurationSeconds { get; set; }

        public int? ScrollPercent { get; set; }

        [StringLength(500)]
        public string ReferrerUrl { get; set; }

        public int? PageLoadMs { get; set; }

        public string ExtraDataJson { get; set; }

        [StringLength(50)]
        public string DeviceType { get; set; } // Mobile, Desktop, Tablet

        [StringLength(50)]
        public string IPAddress { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

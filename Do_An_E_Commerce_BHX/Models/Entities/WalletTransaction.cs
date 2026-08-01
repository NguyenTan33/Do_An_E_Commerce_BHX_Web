using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    [Table("WalletTransaction")]
    public class WalletTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(128)]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // 0: Hoàn tiền đơn hàng (+), 1: Yêu cầu rút tiền (-), 2: Hoàn lại tiền từ yêu cầu rút thất bại (+)
        public int TransactionType { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal")]
        public decimal BalanceAfter { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; }

        public int? OrderId { get; set; }

        public int? WithdrawalRequestId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

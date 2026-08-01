using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    [Table("WithdrawalRequest")]
    public class WithdrawalRequest
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(128)]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        [Required, StringLength(100)]
        public string BankName { get; set; }

        [Required, StringLength(50)]
        public string AccountNumber { get; set; }

        [Required, StringLength(100)]
        public string AccountHolderName { get; set; }

        // 0: Đang rút (Pending), 1: Thành công (Completed), 2: Thất bại (Rejected)
        public int Status { get; set; } = 0;

        // Ngày dự kiến tiền về tài khoản (Tính tự động theo mốc Thứ 5 lúc 10h)
        public DateTime ExpectedPayoutDate { get; set; }

        [StringLength(500)]
        public string AdminNote { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ProcessedDate { get; set; }
    }
}

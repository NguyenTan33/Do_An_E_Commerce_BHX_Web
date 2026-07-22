using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public int? ShipperId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public double TotalAmount { get; set; }
        public double DiscountAmount { get; set; }
        public double ShippingFee { get; set; }

        // --- THÔNG TIN GIAO HÀNG (Snapshot) ---
        [Required, StringLength(100)]
        public string ReceiverName { get; set; }

        [Required, StringLength(15)]
        public string ReceiverPhone { get; set; }

        [Required, StringLength(255)]
        public string ShippingAddress { get; set; }

        // Số điểm tích được từ đơn hàng này (VD: +10 điểm)
        public int EarnedPoints { get; set; }

        // Số điểm khách đã dùng để giảm giá cho đơn này (VD: -20 điểm)
        public int UsedPoints { get; set; }

        // Số tiền giảm giá quy đổi từ điểm (VD: 20 điểm = 20,000 VNĐ)
        public double PointDiscountAmount { get; set; }

        public string Note { get; set; }

        // --- TRẠNG THÁI ---
        // 0 = COD, 1 = Momo, 2 = VNPay
        public int PaymentMethod { get; set; }

        // 0 = Chờ duyệt, 1 = Đã duyệt, 2 = Đang giao, 3 = Thành công, 4 = Đã hủy
        public int OrderStatus { get; set; }

        // 0 = Chưa thu, 1 = Đã thu
        public int PaymentStatus { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
using System;

namespace Do_An_E_Commerce_BHX.Models
{
    public class PendingCheckoutSession
    {
        public string PendingCode { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ShippingAddress { get; set; }
        public int PaymentMethod { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public int UsedPoints { get; set; }
        public string Note { get; set; }
        public string SelectedIds { get; set; }
        public string CouponCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

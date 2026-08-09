using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Controllers;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IOrderCheckoutService
    {
        Task<(Cart CartData, List<UserAddress> UserAddresses, string UserFullName, string UserPhone, int LoyaltyPoints, List<VoucherEvaluationResult> SuggestedVouchers, decimal DiscountAmount, string AppliedCode, string CouponMessage)>
            GetCheckoutDataAsync(string userId, string selectedIds, string coupon);

        (bool Success, string Message, int OrderId, bool IsPendingSession, PendingCheckoutSession PendingSession)
            CreatePendingCheckoutSession(string userId, string receiverName, string receiverPhone, string shippingAddress,
                int paymentMethod, decimal shippingFee, decimal discountAmount, int usedPoints, string note, string selectedIds, string couponCode);

        Order GetPendingOrderForPaymentView(PendingCheckoutSession pendingSession, string userId);

        (bool Success, string Message, int CreatedOrderId)
            ProcessCODCheckout(string userId, int orderId, PendingCheckoutSession pendingSession);

        (bool Success, bool IsPaid, string Message, int CreatedOrderId)
            ConfirmBankPayment(string userId, int orderId, int paymentMethod, PendingCheckoutSession pendingSession);

        (bool IsPaid, bool IsExpired, int PaymentStatus, int PaymentMethod, string Message)
            CheckPaymentStatus(string userId, int orderId, PendingCheckoutSession pendingSession, int? lastCreatedOrderId);

        void LogSePay(string message);
    }
}

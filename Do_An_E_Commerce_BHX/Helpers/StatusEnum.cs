using System;
using System.Web;

namespace Do_An_E_Commerce_BHX.Helpers
{
    /// <summary>
    /// Các Enum chuẩn hóa trạng thái Đơn hàng, Rút tiền, Ví tiền và Phương thức thanh toán
    /// </summary>
    public enum OrderStatusEnum
    {
        Pending = 0,     // Chờ xác nhận
        Packing = 1,     // Đang đóng gói
        Shipping = 2,    // Đang giao hàng
        Delivered = 3,   // Đã giao hàng
        Completed = 4,   // Đã hoàn thành
        Cancelled = 5    // Đã hủy đơn
    }

    public enum WithdrawalStatusEnum
    {
        Pending = 0,    // Đang rút (Chờ admin xử lý)
        Completed = 1,  // Thành công (Admin đã xác nhận hoàn tiền)
        Rejected = 2    // Thất bại (Admin đã từ chối & hoàn tiền lại ví)
    }

    public enum WalletTransactionTypeEnum
    {
        RefundOrder = 0,    // Hoàn tiền từ đơn hàng hủy (+)
        WithdrawRequest = 1,// Yêu cầu rút tiền về ngân hàng (-)
        WithdrawRefund = 2  // Hoàn tiền lại ví do rút thất bại (+)
    }

    public enum PaymentMethodEnum
    {
        COD = 0,         // Thanh toán khi nhận hàng (Tài xế thu tiền)
        BankTransfer = 1,// Chuyển khoản VietQR (SePay tự động xác nhận)
        BHXWallet = 2,   // Thanh toán bằng Ví cá nhân BHX
        MoMo = 3         // Thanh toán qua Ví MoMo
    }

    /// <summary>
    /// Helper mở rộng hiển thị nhãn và HTML Badge màu sắc chuẩn cho các Enum
    /// </summary>
    public static class StatusHelper
    {
        #region XỬ LÝ TRẠNG THÁI ĐƠN HÀNG (ORDER STATUS)
        public static string GetOrderStatusName(int status)
        {
            switch ((OrderStatusEnum)status)
            {
                case OrderStatusEnum.Pending: return "Chờ xác nhận";
                case OrderStatusEnum.Packing: return "Đang đóng gói";
                case OrderStatusEnum.Shipping: return "Đang giao hàng";
                case OrderStatusEnum.Delivered: return "Đã giao hàng";
                case OrderStatusEnum.Completed: return "Đã hoàn thành";
                case OrderStatusEnum.Cancelled: return "Đã hủy đơn";
                default: return "Không xác định";
            }
        }

        public static HtmlString GetOrderStatusBadgeHtml(int status)
        {
            string html = "";
            switch ((OrderStatusEnum)status)
            {
                case OrderStatusEnum.Pending:
                    html = "<span class=\"badge bg-warning text-dark px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-clock me-1\"></i>Chờ xác nhận</span>";
                    break;
                case OrderStatusEnum.Packing:
                    html = "<span class=\"badge bg-info text-dark px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-box-open me-1\"></i>Đang đóng gói</span>";
                    break;
                case OrderStatusEnum.Shipping:
                    html = "<span class=\"badge bg-primary text-white px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-truck-fast me-1\"></i>Đang giao hàng</span>";
                    break;
                case OrderStatusEnum.Delivered:
                    html = "<span class=\"badge bg-success text-white px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-circle-check me-1\"></i>Đã giao hàng</span>";
                    break;
                case OrderStatusEnum.Completed:
                    html = "<span class=\"badge bg-success text-white px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-square-check me-1\"></i>Đã hoàn thành</span>";
                    break;
                case OrderStatusEnum.Cancelled:
                    html = "<span class=\"badge bg-danger text-white px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-ban me-1\"></i>Đã hủy đơn</span>";
                    break;
                default:
                    html = "<span class=\"badge bg-secondary text-white px-3 py-2 rounded-pill fw-bold\">Không xác định</span>";
                    break;
            }
            return new HtmlString(html);
        }
        #endregion

        #region XỬ LÝ TRẠNG THÁI RÚT TIỀN (WITHDRAWAL STATUS)
        public static string GetWithdrawalStatusName(int status)
        {
            switch ((WithdrawalStatusEnum)status)
            {
                case WithdrawalStatusEnum.Pending: return "Đang rút (Chờ duyệt)";
                case WithdrawalStatusEnum.Completed: return "Thành công";
                case WithdrawalStatusEnum.Rejected: return "Từ chối / Thất bại";
                default: return "Chờ xử lý";
            }
        }

        public static HtmlString GetWithdrawalStatusBadgeHtml(int status)
        {
            string html = "";
            switch ((WithdrawalStatusEnum)status)
            {
                case WithdrawalStatusEnum.Pending:
                    html = "<span class=\"badge bg-warning text-dark px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-spinner fa-spin me-1\"></i>Đang rút</span>";
                    break;
                case WithdrawalStatusEnum.Completed:
                    html = "<span class=\"badge bg-success text-white px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-circle-check me-1\"></i>Thành công</span>";
                    break;
                case WithdrawalStatusEnum.Rejected:
                    html = "<span class=\"badge bg-danger text-white px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-circle-xmark me-1\"></i>Hủy / Thất bại</span>";
                    break;
                default:
                    html = "<span class=\"badge bg-secondary text-white px-3 py-2 rounded-pill fw-bold\">Không xác định</span>";
                    break;
            }
            return new HtmlString(html);
        }
        #endregion

        #region XỬ LÝ NHẬT KÝ VÍ TIỀN (WALLET TRANSACTION TYPE)
        public static string GetTransactionTypeName(int type)
        {
            switch ((WalletTransactionTypeEnum)type)
            {
                case WalletTransactionTypeEnum.RefundOrder: return "Hoàn tiền đơn hủy";
                case WalletTransactionTypeEnum.WithdrawRequest: return "Yêu cầu rút tiền";
                case WalletTransactionTypeEnum.WithdrawRefund: return "Hoàn tiền rút thất bại";
                default: return "Biến động số dư";
            }
        }

        public static HtmlString GetTransactionTypeBadgeHtml(int type)
        {
            string html = "";
            switch ((WalletTransactionTypeEnum)type)
            {
                case WalletTransactionTypeEnum.RefundOrder:
                    html = "<span class=\"badge bg-success bg-opacity-25 text-success border border-success px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-arrow-down-left me-1\"></i>+ Hoàn tiền đơn hủy</span>";
                    break;
                case WalletTransactionTypeEnum.WithdrawRequest:
                    html = "<span class=\"badge bg-danger bg-opacity-25 text-danger border border-danger px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-arrow-up-right me-1\"></i>- Yêu cầu rút tiền</span>";
                    break;
                case WalletTransactionTypeEnum.WithdrawRefund:
                    html = "<span class=\"badge bg-info bg-opacity-25 text-info border border-info px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-rotate-left me-1\"></i>+ Hoàn lại ví</span>";
                    break;
                default:
                    html = "<span class=\"badge bg-secondary text-white px-3 py-2 rounded-pill fw-bold\">Biến động số dư</span>";
                    break;
            }
            return new HtmlString(html);
        }
        #endregion

        #region XỬ LÝ PHƯƠNG THỨC THANH TOÁN (PAYMENT METHOD)
        public static string GetPaymentMethodName(string pMethod)
        {
            if (string.IsNullOrEmpty(pMethod)) return "Thanh toán khi nhận hàng (COD)";
            if (pMethod.IndexOf("VietQR", StringComparison.OrdinalIgnoreCase) >= 0 || pMethod.IndexOf("Bank", StringComparison.OrdinalIgnoreCase) >= 0 || pMethod.IndexOf("Chuyển khoản", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Chuyển khoản ngân hàng (VietQR)";
            if (pMethod.IndexOf("Wallet", StringComparison.OrdinalIgnoreCase) >= 0 || pMethod.IndexOf("Ví", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Ví cá nhân BHX";
            if (pMethod.IndexOf("MoMo", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Ví điện tử MoMo";
            return pMethod;
        }

        public static HtmlString GetPaymentMethodBadgeHtml(string pMethod)
        {
            string name = GetPaymentMethodName(pMethod);
            string html = "";
            if (name.Contains("VietQR"))
            {
                html = "<span class=\"badge bg-info bg-opacity-25 text-info border border-info px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-qrcode me-1\"></i>" + name + "</span>";
            }
            else if (name.Contains("Ví"))
            {
                html = "<span class=\"badge bg-warning bg-opacity-25 text-warning border border-warning px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-wallet me-1\"></i>" + name + "</span>";
            }
            else
            {
                html = "<span class=\"badge bg-secondary bg-opacity-25 text-white border border-secondary px-3 py-2 rounded-pill fw-bold\"><i class=\"fa-solid fa-hand-holding-dollar me-1\"></i>" + name + "</span>";
            }
            return new HtmlString(html);
        }
        #endregion
    }
}
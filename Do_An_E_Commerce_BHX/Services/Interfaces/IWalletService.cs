using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IWalletService
    {
        Task<UserWallet> GetOrCreateWalletAsync(string userId);
        Task<bool> RefundOrderToWalletAsync(int orderId, string reason);
        Task<(bool Success, string Message)> RequestWithdrawalAsync(string userId, decimal amount, string bankName, string accountNumber, string accountHolderName);
        Task<(bool Success, string Message)> ProcessWithdrawalRequestAsync(int requestId, bool isApproved, string adminNote = null);
        DateTime CalculateExpectedPayoutDate(DateTime requestDate);
        Task<List<WalletTransaction>> GetUserTransactionsAsync(string userId);
        Task<List<WithdrawalRequest>> GetUserWithdrawalRequestsAsync(string userId);
    }
}

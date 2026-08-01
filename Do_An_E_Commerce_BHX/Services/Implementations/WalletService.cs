using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _db;

        public WalletService(ApplicationDbContext db = null)
        {
            _db = db ?? new ApplicationDbContext();
            ApplicationDbContext.EnsureProductColumnsExist(_db);
        }

        public async Task<UserWallet> GetOrCreateWalletAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var wallet = await _db.UserWallet.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new UserWallet
                {
                    UserId = userId,
                    Balance = 0,
                    UpdatedAt = DateTime.Now
                };
                _db.UserWallet.Add(wallet);
                await _db.SaveChangesAsync();
            }

            return wallet;
        }

        public async Task<bool> RefundOrderToWalletAsync(int orderId, string reason)
        {
            var order = await _db.Order.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || string.IsNullOrEmpty(order.UserId)) return false;

            // Kiểm tra chỉ hoàn tiền đối với đơn hàng đã chuyển khoản thành công (PaymentMethod == 1)
            if (order.PaymentMethod != 1)
            {
                return false;
            }

            // Kiểm tra xem đơn hàng này đã từng được hoàn tiền vào ví chưa
            bool alreadyRefunded = await _db.WalletTransaction.AnyAsync(t => t.OrderId == orderId && t.TransactionType == 0);
            if (alreadyRefunded)
            {
                return true;
            }

            var wallet = await GetOrCreateWalletAsync(order.UserId);
            decimal amount = (decimal)order.TotalAmount;
            decimal balanceBefore = wallet.Balance;

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.Now;

            var tx = new WalletTransaction
            {
                UserId = order.UserId,
                TransactionType = 0, // 0 = Cộng tiền hoàn đơn
                Amount = amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                Description = $"Hoàn tiền đơn hàng #{order.Id} ({reason})",
                OrderId = order.Id,
                CreatedDate = DateTime.Now
            };

            _db.WalletTransaction.Add(tx);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> RequestWithdrawalAsync(string userId, decimal amount, string bankName, string accountNumber, string accountHolderName)
        {
            if (string.IsNullOrEmpty(userId)) return (false, "Bạn chưa đăng nhập!");

            if (amount <= 0) return (false, "Số tiền rút phải lớn hơn 0 VNĐ!");

            var wallet = await GetOrCreateWalletAsync(userId);
            if (wallet == null || wallet.Balance < amount)
            {
                return (false, "Số dư trong ví không đủ để thực hiện rút tiền!");
            }

            if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(accountHolderName))
            {
                return (false, "Vui lòng nhập đầy đủ thông tin Tên chủ tài khoản, Số tài khoản và Ngân hàng thụ hưởng!");
            }

            DateTime expectedPayoutDate = CalculateExpectedPayoutDate(DateTime.Now);
            decimal balanceBefore = wallet.Balance;

            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.Now;

            var request = new WithdrawalRequest
            {
                UserId = userId,
                Amount = amount,
                BankName = bankName.Trim(),
                AccountNumber = accountNumber.Trim(),
                AccountHolderName = accountHolderName.Trim().ToUpper(),
                Status = 0, // 0 = Đang rút (Pending)
                ExpectedPayoutDate = expectedPayoutDate,
                CreatedDate = DateTime.Now
            };

            _db.WithdrawalRequest.Add(request);
            await _db.SaveChangesAsync();

            var tx = new WalletTransaction
            {
                UserId = userId,
                TransactionType = 1, // 1 = Trừ tiền rút
                Amount = amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                Description = $"Rút tiền về NH {bankName.Trim()} - STK: {accountNumber.Trim()} ({accountHolderName.Trim().ToUpper()})",
                WithdrawalRequestId = request.Id,
                CreatedDate = DateTime.Now
            };

            _db.WalletTransaction.Add(tx);
            await _db.SaveChangesAsync();

            return (true, $"Tạo yêu cầu rút tiền {amount:N0} VNĐ thành công! Tiền dự kiến về tài khoản sau 17:00 ngày {expectedPayoutDate:dd/MM/yyyy}.");
        }

        public async Task<(bool Success, string Message)> ProcessWithdrawalRequestAsync(int requestId, bool isApproved, string adminNote = null)
        {
            var request = await _db.WithdrawalRequest.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
            {
                return (false, "Không tìm thấy yêu cầu rút tiền này!");
            }

            if (request.Status != 0)
            {
                return (false, "Yêu cầu rút tiền này đã được xử lý trước đó rồi!");
            }

            if (isApproved)
            {
                request.Status = 1; // 1 = Thành công (Completed)
                request.AdminNote = !string.IsNullOrWhiteSpace(adminNote) ? adminNote : "Đã chuyển khoản thành công.";
                request.ProcessedDate = DateTime.Now;
                await _db.SaveChangesAsync();

                return (true, "Xác nhận chuyển khoản thành công! Trạng thái đã được cập nhật.");
            }
            else
            {
                request.Status = 2; // 2 = Thất bại (Rejected)
                request.AdminNote = !string.IsNullOrWhiteSpace(adminNote) ? adminNote : "Yêu cầu rút tiền bị từ chối.";
                request.ProcessedDate = DateTime.Now;

                // HOÀN LẠI TIỀN VÀO VÍ CÁ NHÂN CỦA KHÁCH HÀNG
                var wallet = await GetOrCreateWalletAsync(request.UserId);
                decimal balanceBefore = wallet.Balance;

                wallet.Balance += request.Amount;
                wallet.UpdatedAt = DateTime.Now;

                var tx = new WalletTransaction
                {
                    UserId = request.UserId,
                    TransactionType = 2, // 2 = Hoàn lại tiền rút thất bại
                    Amount = request.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Description = $"Hoàn lại {request.Amount:N0} VNĐ vào Ví do yêu cầu rút tiền #{request.Id} bị từ chối/thất bại: {request.AdminNote}",
                    WithdrawalRequestId = request.Id,
                    CreatedDate = DateTime.Now
                };

                _db.WalletTransaction.Add(tx);
                await _db.SaveChangesAsync();

                return (true, $"Đã hủy yêu cầu rút tiền và hoàn trả {request.Amount:N0} VNĐ lại vào Ví của khách hàng!");
            }
        }

        public DateTime CalculateExpectedPayoutDate(DateTime requestDate)
        {
            // Quy tắc:
            // Rút TRƯỚC 10:00 sáng Thứ 5 -> Chuyển sau 17:00 chiều Thứ 5 tuần này.
            // Rút SAU 10:00 sáng Thứ 5 -> Chuyển sau 17:00 chiều Thứ 5 tuần sau.
            
            DayOfWeek day = requestDate.DayOfWeek;
            DateTime targetThursday;

            if (day == DayOfWeek.Thursday)
            {
                if (requestDate.TimeOfDay < new TimeSpan(10, 0, 0))
                {
                    targetThursday = requestDate.Date;
                }
                else
                {
                    targetThursday = requestDate.Date.AddDays(7);
                }
            }
            else
            {
                int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)day);
                if (daysUntilThursday < 0)
                {
                    daysUntilThursday += 7;
                }
                targetThursday = requestDate.Date.AddDays(daysUntilThursday);
            }

            return new DateTime(targetThursday.Year, targetThursday.Month, targetThursday.Day, 17, 0, 0);
        }

        public async Task<List<WalletTransaction>> GetUserTransactionsAsync(string userId)
        {
            return await _db.WalletTransaction
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<WithdrawalRequest>> GetUserWithdrawalRequestsAsync(string userId)
        {
            return await _db.WithdrawalRequest
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }
    }
}

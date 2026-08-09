using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Controllers
{
    [Authorize]
    public class WalletController : BaseController
    {
        private readonly IWalletService _walletService;

        public WalletController()
        {
            _walletService = new WalletService(DbContext);
        }

        public WalletController(IWalletService walletService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _walletService = walletService ?? new WalletService(DbContext);
        }

        // GET: /Wallet/Index
        public async Task<ActionResult> Index()
        {
            string userId = User.Identity.GetUserId();
            var wallet = await _walletService.GetOrCreateWalletAsync(userId);
            var transactions = await _walletService.GetUserTransactionsAsync(userId);
            var withdrawalRequests = await _walletService.GetUserWithdrawalRequestsAsync(userId);

            DateTime nextPayoutDate = _walletService.CalculateExpectedPayoutDate(DateTime.Now);

            ViewBag.Transactions = transactions;
            ViewBag.WithdrawalRequests = withdrawalRequests;
            ViewBag.NextPayoutDate = nextPayoutDate;

            return View(wallet);
        }

        // POST: /Wallet/RequestWithdrawal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RequestWithdrawal(string BankName, string AccountNumber, string AccountHolderName, decimal? Amount)
        {
            string userId = User.Identity.GetUserId();
            var wallet = await _walletService.GetOrCreateWalletAsync(userId);

            decimal withdrawAmount = Amount ?? wallet.Balance;

            var result = await _walletService.RequestWithdrawalAsync(userId, withdrawAmount, BankName, AccountNumber, AccountHolderName);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Index");
        }
    }
}

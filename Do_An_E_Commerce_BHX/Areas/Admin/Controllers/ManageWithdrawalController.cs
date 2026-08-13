using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManageWithdrawalController : AdminBaseController
    {
        private readonly IWalletService _walletService;

        public ManageWithdrawalController()
        {
            _walletService = new WalletService(DbContext);
        }

        public ManageWithdrawalController(IWalletService walletService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _walletService = walletService ?? new WalletService(DbContext);
        }

        // GET: /Admin/ManageWithdrawal
        public async Task<ActionResult> Index(string searchKeyword = "", int? statusFilter = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            await SetAdminFullNameViewBagAsync();

            var query = DbContext.WithdrawalRequest
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                string kw = searchKeyword.Trim().ToLower();
                int idSearch = 0;
                bool isId = int.TryParse(kw.Replace("#", ""), out idSearch);

                query = query.Where(r => (isId && r.Id == idSearch) ||
                                         (r.User != null && (r.User.FullName.ToLower().Contains(kw) || r.User.Email.ToLower().Contains(kw) || r.User.PhoneNumber.Contains(kw))) ||
                                         r.BankName.ToLower().Contains(kw) ||
                                         r.AccountNumber.Contains(kw) ||
                                         r.AccountHolderName.ToLower().Contains(kw));
            }

            if (statusFilter.HasValue && statusFilter.Value >= 0)
            {
                query = query.Where(r => r.Status == statusFilter.Value);
            }

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                query = query.Where(r => r.CreatedDate >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.CreatedDate <= endDate);
            }

            var requestsList = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();

            ViewBag.CountPending = await DbContext.WithdrawalRequest.CountAsync(r => r.Status == 0);
            ViewBag.CountCompleted = await DbContext.WithdrawalRequest.CountAsync(r => r.Status == 1);
            ViewBag.CountRejected = await DbContext.WithdrawalRequest.CountAsync(r => r.Status == 2);
            ViewBag.TotalPendingAmount = (await DbContext.WithdrawalRequest.Where(r => r.Status == 0).SumAsync(r => (decimal?)r.Amount)) ?? 0;
            ViewBag.TotalCompletedAmount = (await DbContext.WithdrawalRequest.Where(r => r.Status == 1).SumAsync(r => (decimal?)r.Amount)) ?? 0;

            ViewBag.SearchKeyword = searchKeyword;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(requestsList);
        }

        // POST: /Admin/ManageWithdrawal/Approve
        [HttpPost]
        public async Task<ActionResult> Approve(int id, string adminNote)
        {
            if (!User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện chức năng này!" });
            }

            var result = await _walletService.ProcessWithdrawalRequestAsync(id, isApproved: true, adminNote: adminNote);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Admin/ManageWithdrawal/Reject
        [HttpPost]
        public async Task<ActionResult> Reject(int id, string adminNote)
        {
            if (!User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện chức năng này!" });
            }

            var result = await _walletService.ProcessWithdrawalRequestAsync(id, isApproved: false, adminNote: adminNote);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}

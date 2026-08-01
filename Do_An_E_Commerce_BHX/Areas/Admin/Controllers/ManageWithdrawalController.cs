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
    [Authorize(Roles = "Admin")]
    public class ManageWithdrawalController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly IWalletService _walletService;

        public ManageWithdrawalController()
        {
            _walletService = new WalletService(_db);
        }

        public ManageWithdrawalController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        // GET: /Admin/ManageWithdrawal
        public async Task<ActionResult> Index(string searchKeyword = "", int? statusFilter = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _db.WithdrawalRequest
                .Include(r => r.User)
                .AsQueryable();

            // Lọc nâng cao theo từ khóa (Tên khách hàng, SĐT, Email, Tên chủ TK, Số tài khoản, Ngân hàng)
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

            // Lọc theo Trạng thái
            if (statusFilter.HasValue && statusFilter.Value >= 0)
            {
                query = query.Where(r => r.Status == statusFilter.Value);
            }

            // Lọc theo Khoảng ngày
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

            // THỐNG KÊ TỔNG QUAN
            ViewBag.CountPending = await _db.WithdrawalRequest.CountAsync(r => r.Status == 0);
            ViewBag.CountCompleted = await _db.WithdrawalRequest.CountAsync(r => r.Status == 1);
            ViewBag.CountRejected = await _db.WithdrawalRequest.CountAsync(r => r.Status == 2);
            ViewBag.TotalPendingAmount = (await _db.WithdrawalRequest.Where(r => r.Status == 0).SumAsync(r => (decimal?)r.Amount)) ?? 0;
            ViewBag.TotalCompletedAmount = (await _db.WithdrawalRequest.Where(r => r.Status == 1).SumAsync(r => (decimal?)r.Amount)) ?? 0;

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
            var result = await _walletService.ProcessWithdrawalRequestAsync(id, isApproved: true, adminNote: adminNote);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Admin/ManageWithdrawal/Reject
        [HttpPost]
        public async Task<ActionResult> Reject(int id, string adminNote)
        {
            var result = await _walletService.ProcessWithdrawalRequestAsync(id, isApproved: false, adminNote: adminNote);
            return Json(new { success = result.Success, message = result.Message });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}

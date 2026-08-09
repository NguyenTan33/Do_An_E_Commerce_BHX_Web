using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public abstract class AdminBaseController : Controller
    {
        protected ApplicationDbContext DbContext { get; set; }

        protected AdminBaseController()
        {
            DbContext = new ApplicationDbContext();
        }

        protected AdminBaseController(ApplicationDbContext dbContext)
        {
            DbContext = dbContext ?? new ApplicationDbContext();
        }

        protected async Task SetAdminFullNameViewBagAsync()
        {
            var userId = User?.Identity?.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                ViewBag.FullName = user?.FullName;
            }
        }

        protected void SetAdminFullNameViewBagSync()
        {
            var userId = User?.Identity?.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var user = DbContext.Users.Find(userId);
                ViewBag.FullName = user?.FullName;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && DbContext != null)
            {
                DbContext.Dispose();
                DbContext = null;
            }
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManageUserController : AdminBaseController
    {
        private ApplicationUserManager _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private IUserService _userService;

        public ManageUserController()
        {
        }

        public ManageUserController(ApplicationUserManager userManager, RoleManager<IdentityRole> roleManager, IUserService userService = null, ApplicationDbContext dbContext = null)
            : base(dbContext)
        {
            UserManager = userManager;
            RoleManager = roleManager;
            _userService = userService;
        }

        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }

        public RoleManager<IdentityRole> RoleManager
        {
            get => _roleManager ?? new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(DbContext));
            private set => _roleManager = value;
        }

        public IUserService UserService
        {
            get => _userService ?? new UserService(DbContext, UserManager, RoleManager);
            private set => _userService = value;
        }

        // GET: Admin/ManageUser
        public async Task<ActionResult> Index(string searchString, string roleFilter)
        {
            var userViewModels = await UserService.GetUserViewModelsAsync(searchString, roleFilter);

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentRole = roleFilter;
            ViewBag.RolesList = new SelectList(await DbContext.Roles.AsNoTracking().Select(r => r.Name).ToListAsync());
            ViewBag.AllowManagerViewSensitiveInfo = Do_An_E_Commerce_BHX.Helpers.ManagerSensitiveInfoConfig.AllowManagerViewSensitiveInfo;
            return View(userViewModels);
        }

        // GET: Admin/ManageUser/ThemUser
        [HttpGet]
        public async Task<ActionResult> ThemUser()
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = new SelectList(await DbContext.Roles.AsNoTracking().ToListAsync(), "Name", "Name");
            return View();
        }

        // POST: Admin/ManageUser/ThemUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ThemUser(CreateUserViewModel model)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                var result = await UserService.CreateUserAsync(model);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            ViewBag.Roles = new SelectList(await DbContext.Roles.AsNoTracking().ToListAsync(), "Name", "Name", model.RoleName);
            return View(model);
        }

        // GET: Admin/ManageUser/SuaUser/5
        [HttpGet]
        public async Task<ActionResult> SuaUser(string id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return HttpNotFound();

            var userRoles = await UserManager.GetRolesAsync(user.Id);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                SelectedRole = userRoles.FirstOrDefault()
            };

            ViewBag.Roles = new SelectList(await DbContext.Roles.AsNoTracking().ToListAsync(), "Name", "Name", model.SelectedRole);
            return View(model);
        }

        // POST: Admin/ManageUser/SuaUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SuaUser(EditUserViewModel model)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                var result = await UserService.EditUserAsync(model);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            ViewBag.Roles = new SelectList(await DbContext.Roles.AsNoTracking().ToListAsync(), "Name", "Name", model.SelectedRole);
            return View(model);
        }

        // POST: Admin/ManageUser/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(string userId, string newPassword, string id = null)
        {
            if (!User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện chức năng này!" });
            }

            string targetId = !string.IsNullOrEmpty(userId) ? userId : id;
            if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(newPassword))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var result = await UserService.ResetPasswordAsync(targetId, newPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(targetId);
                string userEmail = user != null ? user.Email : "";
                return Json(new { success = true, message = $"Đã đổi mật khẩu thành công cho tài khoản {userEmail}!" });
            }

            return Json(new { success = false, message = string.Join(", ", result.Errors) });
        }

        // POST: Admin/ManageUser/ToggleLockout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ToggleLockout")]
        public async Task<ActionResult> ToggleLockout(string id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            bool success = await UserService.ToggleLockoutAsync(id);
            if (!success) return HttpNotFound();

            var user = await UserManager.FindByIdAsync(id);
            string userEmail = user != null ? user.Email : "";

            if (user != null && user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow)
            {
                TempData["Success"] = $"Đã khóa tài khoản {userEmail}!";
            }
            else
            {
                TempData["Success"] = $"Đã mở khóa tài khoản {userEmail}!";
            }

            return RedirectToAction("Index");
        }

        // POST: Admin/ManageUser/ToggleLock (Alias)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ToggleLock")]
        public Task<ActionResult> ToggleLock(string id)
        {
            return ToggleLockout(id);
        }

        // POST: Admin/ManageUser/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DeleteUser")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.Id == User.Identity.GetUserId())
                {
                    TempData["Error"] = "Bạn không thể xóa tài khoản Admin đang đăng nhập!";
                    return RedirectToAction("Index");
                }

                await UserManager.DeleteAsync(user);
                TempData["Success"] = $"Đã xóa thành công người dùng {user.Email}!";
            }
            return RedirectToAction("Index");
        }

        // POST: Admin/ManageUser/XoaUser (Alias)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("XoaUser")]
        public Task<ActionResult> XoaUser(string id)
        {
            return DeleteUser(id);
        }

        // GET: Admin/ManageUser/OrderHistory?userId=xxx (Trang Xem lịch sử mua hàng của khách hàng)
        public async Task<ActionResult> OrderHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return HttpNotFound("Không tìm thấy mã khách hàng!");
            }

            var customer = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (customer == null)
            {
                return HttpNotFound("Khách hàng không tồn tại!");
            }

            var ordersQuery = DbContext.Order
                .Include("OrderDetails.Product")
                .Where(o => o.UserId == userId);

            int totalCount = await DbContext.Order.CountAsync(o => o.UserId == userId);
            int processingCount = await DbContext.Order.CountAsync(o => o.UserId == userId && (o.OrderStatus == 0 || o.OrderStatus == 1 || o.OrderStatus == 2));
            int completedCount = await DbContext.Order.CountAsync(o => o.UserId == userId && o.OrderStatus == 3);
            int cancelledCount = await DbContext.Order.CountAsync(o => o.UserId == userId && o.OrderStatus == 4);

            ViewBag.TotalCount = totalCount;
            ViewBag.ProcessingCount = processingCount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.CancelledCount = cancelledCount;

            ViewBag.Customer = customer;
            ViewBag.CustomerName = !string.IsNullOrEmpty(customer.FullName) ? customer.FullName : customer.UserName;

            if (!User.IsInRole("Admin"))
            {
                Func<string, string> maskPhone = p => (string.IsNullOrEmpty(p) || p.Length < 6) ? "*****" : p.Substring(0, 3) + "****" + p.Substring(p.Length - 3);
                Func<string, string> maskEmail = e => {
                    if (string.IsNullOrEmpty(e) || !e.Contains("@")) return "*****";
                    var parts = e.Split('@');
                    string name = parts[0];
                    string maskedName = name.Length <= 2 ? name[0] + "***" : name.Substring(0, 2) + "*****";
                    return maskedName + "@" + parts[1];
                };

                ViewBag.CustomerPhone = maskPhone(customer.PhoneNumber ?? "");
                ViewBag.CustomerEmail = maskEmail(customer.Email ?? "");
            }
            else
            {
                ViewBag.CustomerPhone = customer.PhoneNumber ?? "Chưa cập nhật";
                ViewBag.CustomerEmail = customer.Email;
            }

            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        // POST: Admin/ManageUser/ToggleManagerSensitiveInfo
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult ToggleManagerSensitiveInfo(bool enable)
        {
            Do_An_E_Commerce_BHX.Helpers.ManagerSensitiveInfoConfig.AllowManagerViewSensitiveInfo = enable;
            return Json(new
            {
                success = true,
                isAllowed = enable,
                message = enable
                    ? "Đã CHO PHÉP tài khoản Manager xem SĐT & Email đầy đủ!"
                    : "Đã BẬT BẢO MẬT: Tự động che SĐT & Email với tài khoản Manager!"
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _userManager?.Dispose();
                _roleManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
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
    [Authorize(Roles = "Admin")]
    public class ManageUserController : Controller
    {
        private ApplicationDbContext _db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private IUserService _userService;

        public ManageUserController()
        {
        }

        public ManageUserController(ApplicationUserManager userManager, RoleManager<IdentityRole> roleManager, IUserService userService = null)
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
            get => _roleManager ?? new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_db));
            private set => _roleManager = value;
        }

        private IUserService UserService
        {
            get => _userService ?? new UserService(_db, UserManager, RoleManager);
        }

        // GET: Admin/ManageUser
        public async Task<ActionResult> Index(string tuKhoa, string roleFilter)
        {
            var currentUserId = User.Identity.GetUserId();
            var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId); 
            ViewBag.FullName = currentUser?.FullName;

            var userViewModels = await UserService.GetUserViewModelsAsync(tuKhoa, roleFilter);

            ViewBag.RolesList = new SelectList(await _db.Roles.AsNoTracking().Select(r => r.Name).ToListAsync());
            return View(userViewModels);
        }

        // GET: Admin/ManageUser/ThemUser
        [HttpGet]
        public async Task<ActionResult> ThemUser()
        {
            ViewBag.Roles = new SelectList(await _db.Roles.AsNoTracking().ToListAsync(), "Name", "Name");
            return View();
        }

        // POST: Admin/ManageUser/ThemUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ThemUser(CreateUserViewModel model)
        {
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

            ViewBag.Roles = new SelectList(await _db.Roles.AsNoTracking().ToListAsync(), "Name", "Name", model.RoleName);
            return View(model);
        }

        // GET: Admin/ManageUser/SuaUser/5
        [HttpGet]
        public async Task<ActionResult> SuaUser(string id)
        {
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

            ViewBag.Roles = new SelectList(await _db.Roles.AsNoTracking().ToListAsync(), "Name", "Name", model.SelectedRole);
            return View(model);
        }

        // POST: Admin/ManageUser/SuaUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SuaUser(EditUserViewModel model)
        {
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

            ViewBag.Roles = new SelectList(await _db.Roles.AsNoTracking().ToListAsync(), "Name", "Name", model.SelectedRole);
            return View(model);
        }

        // POST: Admin/ManageUser/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(string userId, string newPassword)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newPassword))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }

            // Xóa password cũ và đặt password mới trực tiếp từ Admin
            var token = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var result = await UserManager.ResetPasswordAsync(user.Id, token, newPassword);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Reset mật khẩu thành công!" });
            }

            return Json(new { success = false, message = string.Join(", ", result.Errors) });
        }

        // POST: Admin/ManageUser/ToggleLock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleLock(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return HttpNotFound();

            // Nếu đang bị khóa -> Mở khóa. Ngược lại -> Khóa 100 năm
            if (user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow)
            {
                await UserManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.MinValue);
            }
            else
            {
                await UserManager.SetLockoutEnabledAsync(user.Id, true);
                await UserManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.UtcNow.AddYears(100));
            }

            return RedirectToAction("Index");
        }

        // POST: Admin/ManageUser/XoaUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XoaUser(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user != null)
            {
                // Không cho phép Admin tự xóa chính mình
                if (user.Id == User.Identity.GetUserId())
                {
                    TempData["Error"] = "Bạn không thể xóa tài khoản Admin đang đăng nhập!";
                    return RedirectToAction("Index");
                }

                await UserManager.DeleteAsync(user);
            }

            return RedirectToAction("Index");
        }

        // GET: Admin/ManageUser/OrderHistory?userId=XXX&statusFilter=all
        public async Task<ActionResult> OrderHistory(string userId, string statusFilter = "all")
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return HttpNotFound("Không tìm thấy mã khách hàng!");
            }

            var customer = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (customer == null)
            {
                return HttpNotFound("Khách hàng không tồn tại!");
            }

            var ordersQuery = _db.Order
                .Include("OrderDetails.Product")
                .Where(o => o.UserId == userId);

            // Thống kê số lượng đơn hàng theo từng trạng thái
            int totalCount = await _db.Order.CountAsync(o => o.UserId == userId);
            int processingCount = await _db.Order.CountAsync(o => o.UserId == userId && (o.OrderStatus == 0 || o.OrderStatus == 1 || o.OrderStatus == 2));
            int completedCount = await _db.Order.CountAsync(o => o.UserId == userId && o.OrderStatus == 3);
            int cancelledCount = await _db.Order.CountAsync(o => o.UserId == userId && o.OrderStatus == 4);

            ViewBag.TotalCount = totalCount;
            ViewBag.ProcessingCount = processingCount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.CancelledCount = cancelledCount;

            statusFilter = (statusFilter ?? "all").ToLower();
            ViewBag.StatusFilter = statusFilter;
            ViewBag.Customer = customer;

            if (statusFilter == "processing")
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == 0 || o.OrderStatus == 1 || o.OrderStatus == 2);
            }
            else if (statusFilter == "completed")
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == 3);
            }
            else if (statusFilter == "cancelled")
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == 4);
            }

            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
                _userManager?.Dispose();
                _roleManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #region ViewModels hỗ trợ
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public int LoyaltyPoints { get; set; }
        public List<string> Roles { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastActivityDate { get; set; }

        public UserPresence Presence => new UserPresence(LastActivityDate);
    }

    public class CreateUserViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string RoleName { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string SelectedRole { get; set; }
    }
    #endregion
}
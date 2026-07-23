using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities; // Thay bằng namespace chứa ApplicationUser nếu cần
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Data.Entity; //  Thêm dòng này ở đầu file ManageUserController.cs

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageUserController : Controller
    {
        private ApplicationDbContext _db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;
        private RoleManager<IdentityRole> _roleManager; // Đổi ApplicationRoleManager thành RoleManager<IdentityRole>

        public ManageUserController()
        {
        }

        public ManageUserController(ApplicationUserManager userManager, RoleManager<IdentityRole> roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
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

        // GET: Admin/ManageUser
        public async Task<ActionResult> Index(string tuKhoa, string roleFilter)
        {
            var currentUserId = User.Identity.GetUserId();
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId); 
            ViewBag.FullName = currentUser?.FullName;

            var usersQuery = _db.Users.AsQueryable();

            // Tìm kiếm theo Email, Username hoặc FullName
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                usersQuery = usersQuery.Where(u => u.UserName.Contains(tuKhoa) ||
                                                   u.Email.Contains(tuKhoa) ||
                                                   u.FullName.Contains(tuKhoa));
            }

            var usersList = await usersQuery.ToListAsync();

            // Ghép Role Name vào từng User để hiển thị
            var userViewModels = new List<UserViewModel>();
            foreach (var user in usersList)
            {
                var roles = await UserManager.GetRolesAsync(user.Id);

                // Lọc theo Role nếu có chọn filter
                if (!string.IsNullOrEmpty(roleFilter) && !roles.Contains(roleFilter))
                {
                    continue;
                }

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList(),
                    IsLocked = user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow
                });
            }

            ViewBag.RolesList = new SelectList(await _db.Roles.Select(r => r.Name).ToListAsync());
            return View(userViewModels);
        }

        // GET: Admin/ManageUser/ThemUser
        [HttpGet]
        public async Task<ActionResult> ThemUser()
        {
            ViewBag.Roles = new SelectList(await _db.Roles.ToListAsync(), "Name", "Name");
            return View();
        }

        // POST: Admin/ManageUser/ThemUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ThemUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.RoleName))
                    {
                        await UserManager.AddToRoleAsync(user.Id, model.RoleName);
                    }
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            ViewBag.Roles = new SelectList(await _db.Roles.ToListAsync(), "Name", "Name", model.RoleName);
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

            ViewBag.Roles = new SelectList(await _db.Roles.ToListAsync(), "Name", "Name", model.SelectedRole);
            return View(model);
        }

        // POST: Admin/ManageUser/SuaUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SuaUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(model.Id);
                if (user == null) return HttpNotFound();

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Email = model.Email;
                user.UserName = model.Email;

                var result = await UserManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Cập nhật lại Role
                    var currentRoles = await UserManager.GetRolesAsync(user.Id);
                    await UserManager.RemoveFromRolesAsync(user.Id, currentRoles.ToArray());

                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        await UserManager.AddToRoleAsync(user.Id, model.SelectedRole);
                    }

                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            ViewBag.Roles = new SelectList(await _db.Roles.ToListAsync(), "Name", "Name", model.SelectedRole);
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
        public List<string> Roles { get; set; }
        public bool IsLocked { get; set; }
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
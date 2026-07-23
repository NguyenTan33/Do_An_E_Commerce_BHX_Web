using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageRoleController : Controller
    {
        private ApplicationDbContext _db = new ApplicationDbContext();
        private RoleManager<IdentityRole> _roleManager;

        public ManageRoleController()
        {
            _roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_db));
        }

        // GET: Admin/ManageRole
        public async Task<ActionResult> Index(string tuKhoa)
        {
            var currentUserId = User.Identity.GetUserId();
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            ViewBag.FullName = currentUser?.FullName;

            var query = _roleManager.Roles;

            // Tìm kiếm theo tên quyền
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(r => r.Name.Contains(tuKhoa));
            }

            var roles = await query.ToListAsync();
            return View(roles);
        }

        // GET: Admin/ManageRole/ThemRole
        [HttpGet]
        public ActionResult ThemRole()
        {
            return View();
        }

        // POST: Admin/ManageRole/ThemRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ThemRole(IdentityRole role)
        {
            if (ModelState.IsValid)
            {
                if (!await _roleManager.RoleExistsAsync(role.Name))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role.Name));
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Quyền này đã tồn tại trong hệ thống!");
                }
            }
            return View(role);
        }

        // GET: Admin/ManageRole/SuaRole/5
        [HttpGet]
        public async Task<ActionResult> SuaRole(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return HttpNotFound();

            return View(role);
        }

        // POST: Admin/ManageRole/SuaRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SuaRole(IdentityRole model)
        {
            if (ModelState.IsValid)
            {
                var role = await _roleManager.FindByIdAsync(model.Id);
                if (role != null)
                {
                    // Kiểm tra trùng tên quyền
                    var roleCheck = await _roleManager.FindByNameAsync(model.Name);
                    if (roleCheck != null && roleCheck.Id != model.Id)
                    {
                        ModelState.AddModelError("", "Tên quyền này đã tồn tại!");
                        return View(model);
                    }

                    role.Name = model.Name;
                    var result = await _roleManager.UpdateAsync(role);

                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }
            return View(model);
        }

        // POST: Admin/ManageRole/XoaRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XoaRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                // Ngăn không cho xóa quyền Admin cao nhất (Tùy chọn an toàn)
                if (role.Name == "Admin")
                {
                    TempData["Error"] = "Không thể xóa quyền Admin cốt lõi của hệ thống!";
                    return RedirectToAction("Index");
                }

                await _roleManager.DeleteAsync(role);
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _roleManager?.Dispose();
                _db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
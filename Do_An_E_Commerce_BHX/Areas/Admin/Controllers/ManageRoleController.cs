using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageRoleController : AdminBaseController
    {
        private readonly IAdminRoleService _roleService;

        public ManageRoleController()
        {
            _roleService = new AdminRoleService(DbContext);
        }

        public ManageRoleController(IAdminRoleService roleService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _roleService = roleService ?? new AdminRoleService(DbContext);
        }

        // GET: Admin/ManageRole
        public async Task<ActionResult> Index(string tuKhoa)
        {
            await SetAdminFullNameViewBagAsync();
            var roles = await _roleService.GetFilteredRolesAsync(tuKhoa);
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
                var (success, errors) = await _roleService.CreateRoleAsync(role.Name);
                if (success)
                {
                    return RedirectToAction("Index");
                }
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            return View(role);
        }

        // GET: Admin/ManageRole/SuaRole/5
        [HttpGet]
        public async Task<ActionResult> SuaRole(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();

            var role = await _roleService.GetRoleByIdAsync(id);
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
                var (success, errors) = await _roleService.UpdateRoleAsync(model.Id, model.Name);
                if (success)
                {
                    return RedirectToAction("Index");
                }
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            return View(model);
        }

        // POST: Admin/ManageRole/XoaRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XoaRole(string id)
        {
            var (success, errorMessage) = await _roleService.DeleteRoleAsync(id);
            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                TempData["Error"] = errorMessage;
            }
            return RedirectToAction("Index");
        }
    }
}
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManageCategoryController : AdminBaseController
    {
        private readonly IAdminCategoryService _categoryService;

        public ManageCategoryController()
        {
            _categoryService = new AdminCategoryService(DbContext);
        }

        public ManageCategoryController(IAdminCategoryService categoryService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _categoryService = categoryService ?? new AdminCategoryService(DbContext);
        }

        // GET: Admin/ManageCategory
        public async Task<ActionResult> Index(string tuKhoa, string sortBy)
        {
            await SetAdminFullNameViewBagAsync();
            var ds = await _categoryService.GetFilteredCategoriesAsync(tuKhoa, sortBy);
            return View(ds);
        }

        // GET: Admin/ManageCategory/ThemDanhMuc
        [HttpGet]
        public ActionResult ThemDanhMuc()
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }
            return View();
        }

        // POST: Admin/ManageCategory/ThemDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ThemDanhMuc(Category themDmMoi)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                await _categoryService.CreateCategoryAsync(themDmMoi);
                return RedirectToAction("Index");
            }

            return View(themDmMoi);
        }

        // GET: Admin/ManageCategory/SuaDanhMuc/5
        [HttpGet]
        public async Task<ActionResult> SuaDanhMuc(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            var danhmuc = await _categoryService.GetCategoryByIdAsync(id);
            if (danhmuc == null)
            {
                return HttpNotFound();
            }

            return View(danhmuc);
        }

        // POST: Admin/ManageCategory/SuaDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SuaDanhMuc(Category danhMucMoi)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                await _categoryService.UpdateCategoryAsync(danhMucMoi);
                return RedirectToAction("Index");
            }

            return View(danhMucMoi);
        }

        // POST: Admin/ManageCategory/XoaDanhMuc/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XoaDanhMuc(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện chức năng này!";
                return RedirectToAction("Index");
            }

            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(id);
            if (!success)
            {
                if (errorMessage == "Không tìm thấy danh mục!")
                {
                    return HttpNotFound();
                }
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }

        // Alias POST: Admin/ManageCategory/XoaDm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XoaDm(int id)
        {
            return await XoaDanhMuc(id);
        }
    }
}
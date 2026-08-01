using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageCategoryController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: Admin/ManageCategory
        public async Task<ActionResult> Index(string tuKhoa, string sortBy)
        {
            var userId = User.Identity.GetUserId();
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            ViewBag.FullName = user?.FullName;

            // Lấy danh sách Danh mục (Category)
            var ds = _db.Category.AsNoTracking().AsQueryable();

            // Lọc theo tên danh mục
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                ds = ds.Where(x => x.Name.Contains(tuKhoa));
            }

            // Sắp xếp
            switch (sortBy)
            {
                case "nameAsc":
                    ds = ds.OrderBy(x => x.Name);
                    break;
                case "nameDesc":
                    ds = ds.OrderByDescending(x => x.Name);
                    break;
                default:
                    ds = ds.OrderBy(x => x.Id);
                    break;
            }

            return View(await ds.ToListAsync());
        }

        // GET: Admin/ManageCategory/ThemDanhMuc
        [HttpGet]
        public ActionResult ThemDanhMuc()
        {
            return View();
        }

        // POST: Admin/ManageCategory/ThemDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ThemDanhMuc(Category themDmMoi)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Add(themDmMoi);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(themDmMoi);
        }

        // GET: Admin/ManageCategory/SuaDanhMuc/5
        [HttpGet]
        public async Task<ActionResult> SuaDanhMuc(int id)
        {
            var danhmuc = await _db.Category.FirstOrDefaultAsync(x => x.Id == id);

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
            if (ModelState.IsValid)
            {
                var danhMuccu = await _db.Category.FirstOrDefaultAsync(x => x.Id == danhMucMoi.Id);
                if (danhMuccu != null)
                {
                    danhMuccu.Name = danhMucMoi.Name;
                }
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(danhMucMoi);
        }

        // POST: Admin/ManageCategory/XoaDanhMuc/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XoaDanhMuc(int id)
        {
            var category = await _db.Category.FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            var hasProducts = await _db.Product.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục này vì đang chứa sản phẩm!";
                return RedirectToAction("Index");
            }

            _db.Category.Remove(category);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db?.Dispose();
            base.Dispose(disposing);
        }
    }
}
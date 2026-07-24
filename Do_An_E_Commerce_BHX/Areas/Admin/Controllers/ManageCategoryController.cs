using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageCategoryController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: Admin/ManageCategory
        public ActionResult Index(string tuKhoa, string sortBy)
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);
            ViewBag.FullName = user?.FullName;

            // Lấy danh sách Danh mục (Category)
            var ds = _db.Category.AsQueryable();

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

            return View(ds.ToList());
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
        public ActionResult ThemDanhMuc(Category themDmMoi)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Add(themDmMoi);
                _db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(themDmMoi);
        }

        // GET: Admin/ManageCategory/SuaDanhMuc/5
        [HttpGet]
        public ActionResult SuaDanhMuc(int id)
        {
            var danhmuc = _db.Category.FirstOrDefault(x => x.Id == id);

            if (danhmuc == null)
            {
                return HttpNotFound();
            }

            return View(danhmuc);
        }

        // POST: Admin/ManageCategory/SuaDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaDanhMuc(Category danhMucMoi)
        {
            if (ModelState.IsValid)
            {
                var danhMuccu = _db.Category.FirstOrDefault(x => x.Id == danhMucMoi.Id);
                if (danhMuccu != null)
                {
                    danhMuccu.Name = danhMucMoi.Name;
                }
                _db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(danhMucMoi);
        }

        // POST: Admin/ManageCategory/XoaDm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaDm(int id)
        {
            var danhmuc = _db.Category.FirstOrDefault(x => x.Id == id);
            if (danhmuc != null)
            {
                _db.Category.Remove(danhmuc);
                _db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
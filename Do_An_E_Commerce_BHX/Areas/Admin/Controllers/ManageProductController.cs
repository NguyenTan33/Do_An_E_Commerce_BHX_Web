using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageProductController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // Cập nhật thêm các tham số bool? để nhận giá trị lọc trạng thái
        public ActionResult Index(string tuKhoa, int? categoryId, decimal? giaTu, decimal? giaDen,
                                  int? tonTu, int? tonDen, bool? isAvailable, bool? isHot,
                                  bool? isBestSeller, bool? isLock, string SortBy)
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);
            ViewBag.FullName = user?.FullName;

            var ds = _db.Product.AsQueryable();

            // Lọc theo Tên sản phẩm
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                ds = ds.Where(x => x.Name.Contains(tuKhoa));
            }

            // Lọc theo Danh mục
            if (categoryId.HasValue)
            {
                ds = ds.Where(x => x.CategoryId == categoryId);
            }

            // Lọc theo Khoảng giá
            if (giaTu.HasValue)
            {
                ds = ds.Where(x => x.Price >= giaTu);
            }
            if (giaDen.HasValue)
            {
                ds = ds.Where(x => x.Price <= giaDen);
            }

            // Lọc theo Khoảng tồn kho
            if (tonTu.HasValue)
            {
                ds = ds.Where(x => x.Quantity >= tonTu);
            }
            if (tonDen.HasValue)
            {
                ds = ds.Where(x => x.Quantity <= tonDen);
            }

            // Lọc theo Trạng thái (Kinh doanh, Hot, Bestseller, Khóa)
            if (isAvailable.HasValue)
            {
                ds = ds.Where(x => x.IsAvailable == isAvailable.Value);
            }
            if (isHot.HasValue)
            {
                ds = ds.Where(x => x.IsHot == isHot.Value);
            }
            if (isBestSeller.HasValue)
            {
                ds = ds.Where(x => x.IsBestSeller == isBestSeller.Value);
            }
            if (isLock.HasValue)
            {
                ds = ds.Where(x => x.IsLock == isLock.Value);
            }

            // Sắp xếp
            switch (SortBy)
            {
                case "nameAsc":
                    ds = ds.OrderBy(x => x.Name);
                    break;
                case "nameDesc":
                    ds = ds.OrderByDescending(x => x.Name);
                    break;
                case "priceAsc":
                    ds = ds.OrderBy(x => x.Price);
                    break;
                case "priceDesc":
                    ds = ds.OrderByDescending(x => x.Price);
                    break;
                case "qtyAsc":
                    ds = ds.OrderBy(x => x.Quantity);
                    break;
                case "qtyDesc":
                    ds = ds.OrderByDescending(x => x.Quantity);
                    break;
                default:
                    ds = ds.OrderBy(x => x.Id);
                    break;
            }

            ViewBag.Category = new SelectList(_db.Category, "Id", "Name");

            return View(ds.ToList());
        }

        [HttpGet]
        public ActionResult ThemSP()
        {
            ViewBag.CategoryId = new SelectList(_db.Category, "Id", "Name");
            return View();
        }

        [HttpPost]
        public ActionResult ThemSP(Product ThemSpMoi)
        {
            if (ModelState.IsValid)
            {
                _db.Product.Add(ThemSpMoi);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CategoryId = new SelectList(_db.Category, "Id", "Name", ThemSpMoi.CategoryId);
            return View(ThemSpMoi);
        }

        [HttpPost]
        public ActionResult XoaSP(int Id)
        {
            var sanpham = _db.Product.FirstOrDefault(x => x.Id == Id);
            if (sanpham != null)
            {
                _db.Product.Remove(sanpham);
                _db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult SuaSP(int Id)
        {
            var sanpham = _db.Product.FirstOrDefault(x => x.Id == Id);
            if (sanpham == null)
            {
                return HttpNotFound();
            }
            ViewBag.Category = new SelectList(_db.Category.ToList(), "Id", "Name", sanpham.CategoryId);
            return View(sanpham);
        }

        [HttpPost]
        public ActionResult SuaSP(Product SanPhamMoi)
        {
            var sanphamcu = _db.Product.FirstOrDefault(x => x.Id == SanPhamMoi.Id);

            if (ModelState.IsValid)
            {
                sanphamcu.Name = SanPhamMoi.Name;
                sanphamcu.Price = SanPhamMoi.Price;
                sanphamcu.Quantity = SanPhamMoi.Quantity;
                sanphamcu.Barcode = SanPhamMoi.Barcode;
                sanphamcu.Description = SanPhamMoi.Description;
                sanphamcu.URLImage = SanPhamMoi.URLImage;
                sanphamcu.IsAvailable = SanPhamMoi.IsAvailable;
                sanphamcu.IsHot = SanPhamMoi.IsHot;
                sanphamcu.IsBestSeller = SanPhamMoi.IsBestSeller;
                sanphamcu.IsLock = SanPhamMoi.IsLock;
                sanphamcu.CategoryId = SanPhamMoi.CategoryId;

                _db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Detail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var sanpham = _db.Product
                             .Include(p => p.Category)
                             .FirstOrDefault(p => p.Id == id);

            if (sanpham == null)
            {
                return HttpNotFound();
            }

            return View(sanpham);
        }
    }
}
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Data.Entity;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManagePromotionController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: Admin/ManagePromotion
        public ActionResult Index(string tuKhoa, string trangThai)
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);
            ViewBag.FullName = user?.FullName;

            // Giả định DbSet trong DbContext của bạn tên là Promotions (hoặc Promotion)
            var ds = _db.Promotion.AsQueryable();

            // Tìm kiếm theo mã Code
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                ds = ds.Where(x => x.Code.Contains(tuKhoa));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                bool isActive = trangThai == "true";
                ds = ds.Where(x => x.IsActive == isActive);
            }

            // Sắp xếp mã mới tạo lên đầu
            ds = ds.OrderByDescending(x => x.Id);

            return View(ds.ToList());
        }

        // GET: Admin/ManagePromotion/ThemPromotion
        [HttpGet]
        public ActionResult ThemPromotion()
        {
            return View();
        }

        // POST: Admin/ManagePromotion/ThemPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemPromotion(Promotion model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng mã
                var checkCode = _db.Promotion.FirstOrDefault(x => x.Code.ToUpper() == model.Code.ToUpper());
                if (checkCode != null)
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại!");
                    return View(model);
                }

                model.Code = model.Code.ToUpper();

                _db.Promotion.Add(model);
                _db.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: Admin/ManagePromotion/SuaPromotion/5
        [HttpGet]
        public ActionResult SuaPromotion(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var promo = _db.Promotion.Find(id);
            if (promo == null) return HttpNotFound();

            return View(promo);
        }

        // POST: Admin/ManagePromotion/SuaPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaPromotion(Promotion model)
        {
            if (ModelState.IsValid)
            {
                var promo = _db.Promotion.Find(model.Id);
                if (promo != null)
                {
                    // Kiểm tra trùng mã nếu đổi tên mã
                    var checkCode = _db.Promotion.FirstOrDefault(x => x.Code.ToUpper() == model.Code.ToUpper() && x.Id != model.Id);
                    if (checkCode != null)
                    {
                        ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại!");
                        return View(model);
                    }

                    promo.Code = model.Code.ToUpper();
                    promo.DiscountValue = model.DiscountValue;
                    promo.percentDiscount = model.percentDiscount;
                    promo.EffectiveDate = model.EffectiveDate;
                    promo.ExpiryDate = model.ExpiryDate;
                    promo.IsActive = model.IsActive;

                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        // POST: Admin/ManagePromotion/XoaPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaPromotion(int id)
        {
            var promo = _db.Promotion.Find(id);
            if (promo != null)
            {
                _db.Promotion.Remove(promo);
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
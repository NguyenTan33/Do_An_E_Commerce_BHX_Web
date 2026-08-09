using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManagePromotionController : AdminBaseController
    {
        private readonly IAdminPromotionService _promotionService;

        public ManagePromotionController()
        {
            _promotionService = new AdminPromotionService(DbContext);
        }

        public ManagePromotionController(IAdminPromotionService promotionService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _promotionService = promotionService ?? new AdminPromotionService(DbContext);
        }

        // GET: Admin/ManagePromotion
        public async Task<ActionResult> Index(string tuKhoa, string trangThai)
        {
            await SetAdminFullNameViewBagAsync();
            var ds = await _promotionService.GetFilteredPromotionsAsync(tuKhoa, trangThai);
            return View(ds);
        }

        private async Task PopulateCategoriesDropdownAsync(int? selectedCategoryId = null)
        {
            var categories = await _promotionService.GetCategoriesListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
        }

        // GET: Admin/ManagePromotion/ThemPromotion
        [HttpGet]
        public async Task<ActionResult> ThemPromotion()
        {
            await PopulateCategoriesDropdownAsync();
            var model = new Promotion
            {
                EffectiveDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsActive = true,
                MinOrderAmount = 0
            };
            return View(model);
        }

        // POST: Admin/ManagePromotion/ThemPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ThemPromotion(Promotion model, string DiscountType)
        {
            if (ModelState.IsValid)
            {
                if (await _promotionService.IsCodeExistsAsync(model.Code))
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại!");
                    await PopulateCategoriesDropdownAsync(model.CategoryId);
                    return View(model);
                }

                await _promotionService.CreatePromotionAsync(model, DiscountType);
                return RedirectToAction("Index");
            }
            await PopulateCategoriesDropdownAsync(model.CategoryId);
            return View(model);
        }

        // GET: Admin/ManagePromotion/SuaPromotion/5
        [HttpGet]
        public async Task<ActionResult> SuaPromotion(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var promo = await _promotionService.GetPromotionByIdAsync(id.Value);
            if (promo == null) return HttpNotFound();

            await PopulateCategoriesDropdownAsync(promo.CategoryId);
            return View(promo);
        }

        // POST: Admin/ManagePromotion/SuaPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SuaPromotion(Promotion model, string DiscountType)
        {
            if (ModelState.IsValid)
            {
                if (await _promotionService.IsCodeExistsAsync(model.Code, model.Id))
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại!");
                    await PopulateCategoriesDropdownAsync(model.CategoryId);
                    return View(model);
                }

                bool success = await _promotionService.UpdatePromotionAsync(model, DiscountType);
                if (success)
                {
                    return RedirectToAction("Index");
                }
            }
            await PopulateCategoriesDropdownAsync(model.CategoryId);
            return View(model);
        }

        // POST: Admin/ManagePromotion/XoaPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XoaPromotion(int id)
        {
            await _promotionService.DeletePromotionAsync(id);
            return RedirectToAction("Index");
        }
    }
}
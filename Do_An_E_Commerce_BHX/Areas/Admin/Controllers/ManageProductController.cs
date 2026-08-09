using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    public class ManageProductController : AdminBaseController
    {
        private readonly IAdminProductService _productService;

        public ManageProductController()
        {
            _productService = new AdminProductService(DbContext);
        }

        public ManageProductController(IAdminProductService productService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _productService = productService ?? new AdminProductService(DbContext);
        }

        public void EnsureProductUnitColumnsExist()
        {
            _productService.EnsureProductUnitColumnsExist();
        }

        public async Task<ActionResult> Index(string tuKhoa, int? categoryId, decimal? giaTu, decimal? giaDen,
                                          int? tonTu, int? tonDen, bool? isAvailable, bool? isHot,
                                          bool? isBestSeller, bool? isLock, int? productType, string SortBy)
        {
            await SetAdminFullNameViewBagAsync();

            var list = await _productService.GetFilteredProductsAsync(
                tuKhoa, categoryId, giaTu, giaDen, tonTu, tonDen,
                isAvailable, isHot, isBestSeller, isLock, productType, SortBy);

            var categories = await _productService.GetCategoriesListAsync();
            ViewBag.Category = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.ProductType = productType;

            return View(list);
        }

        private async Task PopulateProductDropdownsAsync(int? categoryId = null, int? parentProductId = null)
        {
            var categoriesList = await _productService.GetCategoriesListAsync();
            var catSelectList = new SelectList(categoriesList, "Id", "Name", categoryId);

            ViewBag.CategoriesList = catSelectList;
            ViewBag.CategoryId = catSelectList;
            ViewBag.Category = catSelectList;

            var baseProducts = await _productService.GetBaseProductsListAsync();
            ViewBag.ParentProductId = new SelectList(baseProducts, "Id", "Name", parentProductId);
        }

        // POST: /Admin/ManageProduct/UploadProductImage (Hỗ trợ tải ảnh từ thiết bị & chụp ảnh trực tiếp)
        [HttpPost]
        public ActionResult UploadProductImage(HttpPostedFileBase imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string ext = Path.GetExtension(imageFile.FileName).ToLower();
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    string[] allowedExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                    if (!allowedExts.Contains(ext))
                    {
                        return Json(new { success = false, message = "Định dạng file không được hỗ trợ! Chỉ chấp nhận JPG, PNG, WEBP, GIF." });
                    }

                    string uploadDir = Server.MapPath("~/Content/images/products/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    string filename = "prod_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ext;
                    string filePath = Path.Combine(uploadDir, filename);
                    imageFile.SaveAs(filePath);

                    string relativePath = "/Content/images/products/" + filename;
                    return Json(new { success = true, imagePath = relativePath, message = "Tải ảnh thành công!" });
                }
                return Json(new { success = false, message = "Vui lòng chọn tệp hình ảnh!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xử lý ảnh: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> ThemSP()
        {
            await PopulateProductDropdownsAsync();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> ThemSP(Product ThemSpMoi, string extraUnitsJson)
        {
            if (ModelState.IsValid)
            {
                await _productService.CreateProductAsync(ThemSpMoi, extraUnitsJson);
                return RedirectToAction("Index");
            }
            await PopulateProductDropdownsAsync(ThemSpMoi.CategoryId, ThemSpMoi.ParentProductId);
            return View(ThemSpMoi);
        }

        [HttpPost]
        public async Task<ActionResult> XoaSP(int Id)
        {
            await _productService.DeleteProductAsync(Id);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> SuaSP(int Id)
        {
            var sanpham = await _productService.GetProductWithUnitsByIdAsync(Id);
            if (sanpham == null)
            {
                return HttpNotFound();
            }
            await PopulateProductDropdownsAsync(sanpham.CategoryId, sanpham.ParentProductId);
            return View(sanpham);
        }

        [HttpPost]
        public async Task<ActionResult> SuaSP(Product SanPhamMoi, string extraUnitsJson)
        {
            if (ModelState.IsValid)
            {
                bool success = await _productService.UpdateProductAsync(SanPhamMoi, extraUnitsJson);
                if (success)
                {
                    return RedirectToAction("Index");
                }
            }
            await PopulateProductDropdownsAsync(SanPhamMoi.CategoryId, SanPhamMoi.ParentProductId);
            return View(SanPhamMoi);
        }

        [HttpGet]
        public async Task<ActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var sanpham = await _productService.GetProductWithCategoryByIdAsync(id.Value);
            if (sanpham == null)
            {
                return HttpNotFound();
            }

            return View(sanpham);
        }
    }
}
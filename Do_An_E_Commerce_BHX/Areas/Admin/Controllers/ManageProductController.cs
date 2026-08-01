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

        public void EnsureProductUnitColumnsExist()
        {
            try
            {
                string sql = @"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Products')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'Unit')
                            ALTER TABLE [dbo].[Products] ADD [Unit] NVARCHAR(50) DEFAULT N'Cái' NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'UnitMultiplier')
                            ALTER TABLE [dbo].[Products] ADD [UnitMultiplier] INT DEFAULT 1 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'ParentProductId')
                            ALTER TABLE [dbo].[Products] ADD [ParentProductId] INT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'PackagingTag')
                            ALTER TABLE [dbo].[Products] ADD [PackagingTag] NVARCHAR(100) NULL;
                    END

                    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Product')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'Unit')
                            ALTER TABLE [dbo].[Product] ADD [Unit] NVARCHAR(50) DEFAULT N'Cái' NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'UnitMultiplier')
                            ALTER TABLE [dbo].[Product] ADD [UnitMultiplier] INT DEFAULT 1 NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'ParentProductId')
                            ALTER TABLE [dbo].[Product] ADD [ParentProductId] INT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND name = 'PackagingTag')
                            ALTER TABLE [dbo].[Product] ADD [PackagingTag] NVARCHAR(100) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'ProductUnit')
                    BEGIN
                        CREATE TABLE [dbo].[ProductUnit] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [ProductId] INT NOT NULL,
                            [UnitName] NVARCHAR(100) NOT NULL,
                            [Price] DECIMAL(18,2) NOT NULL,
                            [ConversionFactor] INT DEFAULT 1 NOT NULL,
                            [IsDefault] BIT DEFAULT 0 NOT NULL
                        );
                    END
                ";
                _db.Database.ExecuteSqlCommand(sql);
            }
            catch { }
        }

        // Cập nhật thêm các tham số bool? và productType để nhận giá trị lọc trạng thái
        public ActionResult Index(string tuKhoa, int? categoryId, decimal? giaTu, decimal? giaDen,
                                  int? tonTu, int? tonDen, bool? isAvailable, bool? isHot,
                                  bool? isBestSeller, bool? isLock, int? productType, string SortBy)
        {
            EnsureProductUnitColumnsExist();

            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);
            ViewBag.FullName = user?.FullName;

            var ds = _db.Product.Include(p => p.ParentProduct).AsQueryable();

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

            // Lọc theo Loại sản phẩm (1: Sản phẩm gốc, 2: Bài quy cách con Thùng/Lốc)
            if (productType.HasValue)
            {
                if (productType.Value == 1)
                {
                    ds = ds.Where(x => x.ParentProductId == null);
                }
                else if (productType.Value == 2)
                {
                    ds = ds.Where(x => x.ParentProductId != null);
                }
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
                    ds = ds.OrderByDescending(x => x.Id);
                    break;
            }

            ViewBag.Category = new SelectList(_db.Category, "Id", "Name", categoryId);
            ViewBag.ProductType = productType;

            return View(ds.ToList());
        }

        private void PopulateProductDropdowns(int? categoryId = null, int? parentProductId = null)
        {
            var categoriesList = _db.Category.OrderBy(c => c.Name).ToList();
            var catSelectList = new SelectList(categoriesList, "Id", "Name", categoryId);

            ViewBag.CategoriesList = catSelectList;
            ViewBag.CategoryId = catSelectList;
            ViewBag.Category = catSelectList;

            var baseProducts = _db.Product.Where(p => p.ParentProductId == null).OrderBy(p => p.Name).ToList();
            ViewBag.ParentProductId = new SelectList(baseProducts, "Id", "Name", parentProductId);
        }

        // POST: /Admin/ManageProduct/UploadProductImage (Hỗ trợ tải ảnh từ thiết bị & chụp ảnh trực tiếp)
        [HttpPost]
        public ActionResult UploadProductImage(System.Web.HttpPostedFileBase imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string ext = System.IO.Path.GetExtension(imageFile.FileName).ToLower();
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    string[] allowedExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                    if (!allowedExts.Contains(ext))
                    {
                        return Json(new { success = false, message = "Định dạng file không được hỗ trợ! Chỉ chấp nhận JPG, PNG, WEBP, GIF." });
                    }

                    string uploadDir = Server.MapPath("~/Content/images/products/");
                    if (!System.IO.Directory.Exists(uploadDir))
                    {
                        System.IO.Directory.CreateDirectory(uploadDir);
                    }

                    string filename = "prod_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ext;
                    string filePath = System.IO.Path.Combine(uploadDir, filename);
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
        public ActionResult ThemSP()
        {
            PopulateProductDropdowns();
            return View();
        }

        [HttpPost]
        public ActionResult ThemSP(Product ThemSpMoi, string extraUnitsJson)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(ThemSpMoi.Unit)) ThemSpMoi.Unit = "Cái";
                if (ThemSpMoi.UnitMultiplier <= 0) ThemSpMoi.UnitMultiplier = 1;

                _db.Product.Add(ThemSpMoi);
                _db.SaveChanges();

                // Tạo Unit mặc định đầu tiên từ thuộc tính của Product
                _db.ProductUnit.Add(new ProductUnit
                {
                    ProductId = ThemSpMoi.Id,
                    UnitName = ThemSpMoi.Unit,
                    Price = ThemSpMoi.Price,
                    ConversionFactor = ThemSpMoi.UnitMultiplier,
                    IsDefault = true
                });

                if (!string.IsNullOrWhiteSpace(extraUnitsJson))
                {
                    try
                    {
                        var extraUnits = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ProductUnit>>(extraUnitsJson);
                        if (extraUnits != null && extraUnits.Any())
                        {
                            foreach (var u in extraUnits)
                            {
                                if (!string.IsNullOrWhiteSpace(u.UnitName) && u.Price > 0)
                                {
                                    _db.ProductUnit.Add(new ProductUnit
                                    {
                                        ProductId = ThemSpMoi.Id,
                                        UnitName = u.UnitName,
                                        Price = u.Price,
                                        ConversionFactor = u.ConversionFactor > 0 ? u.ConversionFactor : 1,
                                        IsDefault = false
                                    });
                                }
                            }
                        }
                    }
                    catch { }
                }

                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            PopulateProductDropdowns(ThemSpMoi.CategoryId, ThemSpMoi.ParentProductId);
            return View(ThemSpMoi);
        }

        [HttpPost]
        public ActionResult XoaSP(int Id)
        {
            var sanpham = _db.Product.FirstOrDefault(x => x.Id == Id);
            if (sanpham != null)
            {
                var units = _db.ProductUnit.Where(u => u.ProductId == Id).ToList();
                if (units.Any()) _db.ProductUnit.RemoveRange(units);

                _db.Product.Remove(sanpham);
                _db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult SuaSP(int Id)
        {
            var sanpham = _db.Product.Include(p => p.ProductUnits).FirstOrDefault(x => x.Id == Id);
            if (sanpham == null)
            {
                return HttpNotFound();
            }
            PopulateProductDropdowns(sanpham.CategoryId, sanpham.ParentProductId);
            return View(sanpham);
        }

        [HttpPost]
        public ActionResult SuaSP(Product SanPhamMoi, string extraUnitsJson)
        {
            var sanphamcu = _db.Product.Include(p => p.ProductUnits).FirstOrDefault(x => x.Id == SanPhamMoi.Id);

            if (sanphamcu != null && ModelState.IsValid)
            {
                sanphamcu.Name = SanPhamMoi.Name;
                sanphamcu.Price = SanPhamMoi.Price;
                sanphamcu.Quantity = SanPhamMoi.Quantity;
                sanphamcu.Unit = !string.IsNullOrEmpty(SanPhamMoi.Unit) ? SanPhamMoi.Unit : "Cái";
                sanphamcu.UnitMultiplier = SanPhamMoi.UnitMultiplier > 0 ? SanPhamMoi.UnitMultiplier : 1;
                sanphamcu.ParentProductId = SanPhamMoi.ParentProductId;
                sanphamcu.PackagingTag = SanPhamMoi.PackagingTag;
                sanphamcu.Barcode = SanPhamMoi.Barcode;
                sanphamcu.Description = SanPhamMoi.Description;
                sanphamcu.URLImage = SanPhamMoi.URLImage;
                sanphamcu.IsAvailable = SanPhamMoi.IsAvailable;
                sanphamcu.IsHot = SanPhamMoi.IsHot;
                sanphamcu.IsBestSeller = SanPhamMoi.IsBestSeller;
                sanphamcu.IsLock = SanPhamMoi.IsLock;
                sanphamcu.CategoryId = SanPhamMoi.CategoryId;

                // Xóa quy cách cũ và cập nhật danh sách mới
                var oldUnits = _db.ProductUnit.Where(u => u.ProductId == sanphamcu.Id).ToList();
                if (oldUnits.Any())
                {
                    _db.ProductUnit.RemoveRange(oldUnits);
                }

                _db.ProductUnit.Add(new ProductUnit
                {
                    ProductId = sanphamcu.Id,
                    UnitName = sanphamcu.Unit,
                    Price = sanphamcu.Price,
                    ConversionFactor = sanphamcu.UnitMultiplier,
                    IsDefault = true
                });

                if (!string.IsNullOrWhiteSpace(extraUnitsJson))
                {
                    try
                    {
                        var extraUnits = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ProductUnit>>(extraUnitsJson);
                        if (extraUnits != null && extraUnits.Any())
                        {
                            foreach (var u in extraUnits)
                            {
                                if (!string.IsNullOrWhiteSpace(u.UnitName) && u.Price > 0 && u.UnitName != sanphamcu.Unit)
                                {
                                    _db.ProductUnit.Add(new ProductUnit
                                    {
                                        ProductId = sanphamcu.Id,
                                        UnitName = u.UnitName,
                                        Price = u.Price,
                                        ConversionFactor = u.ConversionFactor > 0 ? u.ConversionFactor : 1,
                                        IsDefault = false
                                    });
                                }
                            }
                        }
                    }
                    catch { }
                }

                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            PopulateProductDropdowns(SanPhamMoi.CategoryId, SanPhamMoi.ParentProductId);
            return View(SanPhamMoi);
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
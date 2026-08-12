using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Newtonsoft.Json;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminProductService : IAdminProductService
    {
        private readonly ApplicationDbContext _db;

        public AdminProductService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
        }

        public void EnsureProductUnitColumnsExist()
        {
            ApplicationDbContext.EnsureProductColumnsExist(_db);
        }

        public async Task<List<Product>> GetFilteredProductsAsync(
            string tuKhoa, int? categoryId, decimal? giaTu, decimal? giaDen,
            int? tonTu, int? tonDen, bool? isAvailable, bool? isHot,
            bool? isBestSeller, bool? isLock, int? productType, string sortBy)
        {
            EnsureProductUnitColumnsExist();

            var ds = _db.Product.Include(p => p.ParentProduct).AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                ds = ds.Where(x => x.Name.Contains(tuKhoa));
            }

            if (categoryId.HasValue)
            {
                ds = ds.Where(x => x.CategoryId == categoryId);
            }

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

            if (giaTu.HasValue)
            {
                ds = ds.Where(x => x.Price >= giaTu);
            }
            if (giaDen.HasValue)
            {
                ds = ds.Where(x => x.Price <= giaDen);
            }

            if (tonTu.HasValue)
            {
                ds = ds.Where(x => x.Quantity >= tonTu);
            }
            if (tonDen.HasValue)
            {
                ds = ds.Where(x => x.Quantity <= tonDen);
            }

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

            switch (sortBy)
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

            return await ds.ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _db.Product.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> GetProductWithCategoryByIdAsync(int id)
        {
            return await _db.Product
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> GetProductWithUnitsByIdAsync(int id)
        {
            return await _db.Product
                .Include(p => p.ProductUnits)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Category>> GetCategoriesListAsync()
        {
            return await _db.Category.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<List<Product>> GetBaseProductsListAsync()
        {
            return await _db.Product.Where(p => p.ParentProductId == null).OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<bool> CreateProductAsync(Product product, string extraUnitsJson)
        {
            if (string.IsNullOrEmpty(product.Unit)) product.Unit = "Cái";
            if (product.UnitMultiplier <= 0) product.UnitMultiplier = 1;

            _db.Product.Add(product);
            await _db.SaveChangesAsync();

            _db.ProductUnit.Add(new ProductUnit
            {
                ProductId = product.Id,
                UnitName = product.Unit,
                Price = product.Price,
                ConversionFactor = product.UnitMultiplier,
                IsDefault = true
            });

            if (!string.IsNullOrWhiteSpace(extraUnitsJson))
            {
                try
                {
                    var extraUnits = JsonConvert.DeserializeObject<List<ProductUnit>>(extraUnitsJson);
                    if (extraUnits != null && extraUnits.Any())
                    {
                        foreach (var u in extraUnits)
                        {
                            if (!string.IsNullOrWhiteSpace(u.UnitName) && u.Price > 0)
                            {
                                _db.ProductUnit.Add(new ProductUnit
                                {
                                    ProductId = product.Id,
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

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProductAsync(Product product, string extraUnitsJson)
        {
            var existing = await _db.Product.Include(p => p.ProductUnits).FirstOrDefaultAsync(x => x.Id == product.Id);
            if (existing == null) return false;

            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.Quantity = product.Quantity;
            existing.Unit = !string.IsNullOrEmpty(product.Unit) ? product.Unit : "Cái";
            existing.UnitMultiplier = product.UnitMultiplier > 0 ? product.UnitMultiplier : 1;
            existing.ParentProductId = product.ParentProductId;
            existing.PackagingTag = product.PackagingTag;
            existing.Barcode = product.Barcode;
            existing.Description = product.Description;
            existing.URLImage = product.URLImage;
            existing.IsAvailable = product.IsAvailable;
            existing.IsHot = product.IsHot;
            existing.IsBestSeller = product.IsBestSeller;
            existing.IsLock = product.IsLock;
            existing.CategoryId = product.CategoryId;

            var oldUnits = await _db.ProductUnit.Where(u => u.ProductId == existing.Id).ToListAsync();
            if (oldUnits.Any())
            {
                _db.ProductUnit.RemoveRange(oldUnits);
            }

            _db.ProductUnit.Add(new ProductUnit
            {
                ProductId = existing.Id,
                UnitName = existing.Unit,
                Price = existing.Price,
                ConversionFactor = existing.UnitMultiplier,
                IsDefault = true
            });

            if (!string.IsNullOrWhiteSpace(extraUnitsJson))
            {
                try
                {
                    var extraUnits = JsonConvert.DeserializeObject<List<ProductUnit>>(extraUnitsJson);
                    if (extraUnits != null && extraUnits.Any())
                    {
                        foreach (var u in extraUnits)
                        {
                            if (!string.IsNullOrWhiteSpace(u.UnitName) && u.Price > 0 && u.UnitName != existing.Unit)
                            {
                                _db.ProductUnit.Add(new ProductUnit
                                {
                                    ProductId = existing.Id,
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

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _db.Product.FirstOrDefaultAsync(x => x.Id == id);
            if (product != null)
            {
                var units = await _db.ProductUnit.Where(u => u.ProductId == id).ToListAsync();
                if (units.Any()) _db.ProductUnit.RemoveRange(units);

                _db.Product.Remove(product);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}

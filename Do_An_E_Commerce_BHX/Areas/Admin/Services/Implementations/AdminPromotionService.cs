using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminPromotionService : IAdminPromotionService
    {
        private readonly ApplicationDbContext _db;
        private readonly VoucherService _voucherService;

        public AdminPromotionService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
            _voucherService = new VoucherService(_db);
        }

        public void SeedSampleVouchersIfEmpty()
        {
            _voucherService.SeedSampleVouchersIfEmpty();
        }

        public async Task<List<Promotion>> GetFilteredPromotionsAsync(string tuKhoa, string trangThai)
        {
            SeedSampleVouchersIfEmpty();

            var ds = _db.Promotion.AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                ds = ds.Where(x => x.Code.Contains(tuKhoa));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                bool isActive = trangThai == "true";
                ds = ds.Where(x => x.IsActive == isActive);
            }

            return await ds.OrderByDescending(x => x.Id).ToListAsync();
        }

        public async Task<Promotion> GetPromotionByIdAsync(int id)
        {
            return await _db.Promotion.FindAsync(id);
        }

        public async Task<List<Category>> GetCategoriesListAsync()
        {
            return await _db.Category.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<bool> IsCodeExistsAsync(string code, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string codeUpper = code.Trim().ToUpper();

            if (excludeId.HasValue)
            {
                return await _db.Promotion.AnyAsync(x => x.Code.ToUpper() == codeUpper && x.Id != excludeId.Value);
            }
            return await _db.Promotion.AnyAsync(x => x.Code.ToUpper() == codeUpper);
        }

        public async Task<bool> CreatePromotionAsync(Promotion model, string discountType)
        {
            model.Code = model.Code.ToUpper();

            if (discountType == "PERCENT")
            {
                model.percentDiscount = model.DiscountValue;
                model.DiscountValue = 0;
            }
            else
            {
                model.percentDiscount = 0;
            }

            _db.Promotion.Add(model);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePromotionAsync(Promotion model, string discountType)
        {
            var promo = await _db.Promotion.FindAsync(model.Id);
            if (promo == null) return false;

            promo.Code = model.Code.ToUpper();

            if (discountType == "PERCENT")
            {
                promo.percentDiscount = model.DiscountValue;
                promo.DiscountValue = 0;
            }
            else
            {
                promo.DiscountValue = model.DiscountValue;
                promo.percentDiscount = 0;
            }

            promo.MinOrderAmount = model.MinOrderAmount;
            promo.CategoryId = model.CategoryId;
            promo.MaxDiscountAmount = model.MaxDiscountAmount;
            promo.Description = model.Description;
            promo.UsageLimit = model.UsageLimit > 0 ? model.UsageLimit : 100;
            promo.EffectiveDate = model.EffectiveDate;
            promo.ExpiryDate = model.ExpiryDate;
            promo.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePromotionAsync(int id)
        {
            var promo = await _db.Promotion.FindAsync(id);
            if (promo != null)
            {
                _db.Promotion.Remove(promo);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}

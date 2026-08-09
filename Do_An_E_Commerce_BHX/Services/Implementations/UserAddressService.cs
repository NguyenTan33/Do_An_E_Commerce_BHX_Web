using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Interfaces;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class UserAddressService : IUserAddressService
    {
        private readonly ApplicationDbContext _db;

        public UserAddressService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
        }

        public async Task<List<UserAddress>> GetUserAddressesAsync(string userId)
        {
            return await _db.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();
        }

        public async Task<UserAddress> GetUserAddressByIdAsync(int id, string userId)
        {
            return await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        }

        public async Task<bool> CreateAddressAsync(UserAddress model, string userId)
        {
            model.UserId = userId;

            var hasAddress = await _db.UserAddresses.AnyAsync(a => a.UserId == userId);
            if (!hasAddress || model.IsDefault)
            {
                var oldDefaults = await _db.UserAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                foreach (var item in oldDefaults)
                {
                    item.IsDefault = false;
                }
                model.IsDefault = true;
            }

            _db.UserAddresses.Add(model);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAddressAsync(UserAddress model, string userId)
        {
            var addressInDb = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);
            if (addressInDb == null) return false;

            addressInDb.AddressName = model.AddressName;
            addressInDb.ReceiverName = model.ReceiverName;
            addressInDb.ReceiverPhone = model.ReceiverPhone;
            addressInDb.DetailedAddress = model.DetailedAddress;

            if (model.IsDefault && !addressInDb.IsDefault)
            {
                var oldDefaults = await _db.UserAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                foreach (var item in oldDefaults)
                {
                    item.IsDefault = false;
                }
                addressInDb.IsDefault = true;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetDefaultAddressAsync(int id, string userId)
        {
            var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address != null)
            {
                var allAddresses = await _db.UserAddresses.Where(a => a.UserId == userId).ToListAsync();
                foreach (var item in allAddresses)
                {
                    item.IsDefault = (item.Id == id);
                }
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAddressAsync(int id, string userId)
        {
            var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address != null)
            {
                bool wasDefault = address.IsDefault;
                _db.UserAddresses.Remove(address);
                await _db.SaveChangesAsync();

                if (wasDefault)
                {
                    var firstAddress = await _db.UserAddresses.FirstOrDefaultAsync(a => a.UserId == userId);
                    if (firstAddress != null)
                    {
                        firstAddress.IsDefault = true;
                        await _db.SaveChangesAsync();
                    }
                }
                return true;
            }
            return false;
        }
    }
}

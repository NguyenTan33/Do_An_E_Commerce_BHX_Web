using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IUserAddressService
    {
        Task<List<UserAddress>> GetUserAddressesAsync(string userId);
        Task<UserAddress> GetUserAddressByIdAsync(int id, string userId);
        Task<bool> CreateAddressAsync(UserAddress model, string userId);
        Task<bool> UpdateAddressAsync(UserAddress model, string userId);
        Task<bool> SetDefaultAddressAsync(int id, string userId);
        Task<bool> DeleteAddressAsync(int id, string userId);
    }
}

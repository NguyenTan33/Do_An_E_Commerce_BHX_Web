using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Controllers;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IUserService
    {
        Task EnsureDefaultUserRoleAssignedAsync(string userId);
        Task UpdateLastActivityAsync(string userId);
        Task<List<UserViewModel>> GetUserViewModelsAsync(string tuKhoa, string roleFilter);
        Task<IdentityResult> CreateUserAsync(CreateUserViewModel model);
        Task<IdentityResult> EditUserAsync(EditUserViewModel model);
        Task<bool> ToggleLockoutAsync(string userId);
        Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword);
        Task<List<Order>> GetCustomerOrderHistoryAsync(string userId, string statusFilter);
    }
}

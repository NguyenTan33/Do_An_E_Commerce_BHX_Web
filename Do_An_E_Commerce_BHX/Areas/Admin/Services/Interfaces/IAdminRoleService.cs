using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminRoleService
    {
        Task<List<IdentityRole>> GetFilteredRolesAsync(string tuKhoa);
        Task<IdentityRole> GetRoleByIdAsync(string id);
        Task<(bool Success, IEnumerable<string> Errors)> CreateRoleAsync(string roleName);
        Task<(bool Success, IEnumerable<string> Errors)> UpdateRoleAsync(string id, string newRoleName);
        Task<(bool Success, string ErrorMessage)> DeleteRoleAsync(string id);
    }
}

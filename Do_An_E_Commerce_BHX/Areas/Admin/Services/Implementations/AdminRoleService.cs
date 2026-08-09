using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminRoleService : IAdminRoleService
    {
        private readonly ApplicationDbContext _db;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminRoleService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
            _roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_db));
        }

        public AdminRoleService(RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
            _roleManager = roleManager;
        }

        public async Task<List<IdentityRole>> GetFilteredRolesAsync(string tuKhoa)
        {
            var query = _roleManager.Roles;

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(r => r.Name.Contains(tuKhoa));
            }

            return await query.ToListAsync();
        }

        public async Task<IdentityRole> GetRoleByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return await _roleManager.FindByIdAsync(id);
        }

        public async Task<(bool Success, IEnumerable<string> Errors)> CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return (false, new[] { "Tên quyền không được để trống!" });
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return (false, new[] { "Quyền này đã tồn tại trong hệ thống!" });
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            return (result.Succeeded, result.Errors);
        }

        public async Task<(bool Success, IEnumerable<string> Errors)> UpdateRoleAsync(string id, string newRoleName)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return (false, new[] { "Không tìm thấy quyền này!" });
            }

            var roleCheck = await _roleManager.FindByNameAsync(newRoleName);
            if (roleCheck != null && roleCheck.Id != id)
            {
                return (false, new[] { "Tên quyền này đã tồn tại!" });
            }

            role.Name = newRoleName;
            var result = await _roleManager.UpdateAsync(role);
            return (result.Succeeded, result.Errors);
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteRoleAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                if (role.Name == "Admin")
                {
                    return (false, "Không thể xóa quyền Admin cốt lõi của hệ thống!");
                }

                await _roleManager.DeleteAsync(role);
                return (true, null);
            }
            return (false, "Không tìm thấy quyền cần xóa!");
        }
    }
}

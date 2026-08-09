using System;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class AccountAuthService : IAccountAuthService
    {
        private readonly ApplicationDbContext _db;

        public AccountAuthService(ApplicationDbContext db)
        {
            _db = db ?? new ApplicationDbContext();
        }

        public async Task EnsureDefaultUserRoleAssignedAsync(ApplicationUserManager userManager, string userId)
        {
            if (userManager == null || string.IsNullOrEmpty(userId)) return;

            try
            {
                var roleStore = new RoleStore<IdentityRole>(_db);
                var roleManager = new RoleManager<IdentityRole>(roleStore);

                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                }

                var roles = await userManager.GetRolesAsync(userId);
                if (!roles.Contains("User"))
                {
                    await userManager.AddToRoleAsync(userId, "User");
                }
            }
            catch { }
        }

        public async Task RecordLastActivityAsync(ApplicationUserManager userManager, string email)
        {
            if (userManager == null || string.IsNullOrWhiteSpace(email)) return;

            try
            {
                var user = await userManager.FindByEmailAsync(email) ?? await userManager.FindByNameAsync(email);
                if (user != null)
                {
                    user.LastActivityDate = DateTime.Now;
                    await userManager.UpdateAsync(user);
                }
            }
            catch { }
        }
    }
}

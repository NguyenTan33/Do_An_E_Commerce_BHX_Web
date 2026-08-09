using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Services.Interfaces
{
    public interface IAccountAuthService
    {
        Task EnsureDefaultUserRoleAssignedAsync(ApplicationUserManager userManager, string userId);
        Task RecordLastActivityAsync(ApplicationUserManager userManager, string email);
    }
}

using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Do_An_E_Commerce_BHX.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            System.Data.Entity.Database.SetInitializer<ApplicationDbContext>(null);

            // Tự động kiểm tra và khởi tạo cấu trúc bảng / cột nếu cần
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    ApplicationDbContext.EnsureProductColumnsExist(db);
                }
            }
            catch { }

            // Khởi tạo các Role và Tài khoản Demo Trải Nghiệm
            SeedRolesAndDemoAccounts();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private static void SeedRolesAndDemoAccounts()
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    var userStore = new UserStore<ApplicationUser>(db);
                    var userManager = new UserManager<ApplicationUser>(userStore);
                    var roleStore = new RoleStore<IdentityRole>(db);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);

                    // Ensure Roles Exist
                    string[] roles = new[] { "Admin", "Manager", "User" };
                    foreach (var role in roles)
                    {
                        if (!roleManager.RoleExists(role))
                        {
                            roleManager.Create(new IdentityRole(role));
                        }
                    }

                    // Ensure Demo Manager Account (manager@bhx.com / Manager123@)
                    var managerUser = userManager.FindByEmail("manager@bhx.com");
                    if (managerUser == null)
                    {
                        managerUser = new ApplicationUser
                        {
                            UserName = "manager@bhx.com",
                            Email = "manager@bhx.com",
                            FullName = "Nguyễn Văn Manager",
                            Address = "Hệ Thống Quản Lý Bách Hóa Xanh",
                            EmailConfirmed = true
                        };
                        var createResult = userManager.Create(managerUser, "Manager123@");
                        if (createResult.Succeeded)
                        {
                            userManager.AddToRole(managerUser.Id, "Manager");
                        }
                    }
                    else
                    {
                        if (!userManager.IsInRole(managerUser.Id, "Manager"))
                        {
                            userManager.AddToRole(managerUser.Id, "Manager");
                        }
                    }

                    // Ensure Demo Admin Account (admin@bhx.com / Admin123@)
                    var adminUser = userManager.FindByEmail("admin@bhx.com");
                    if (adminUser == null)
                    {
                        adminUser = new ApplicationUser
                        {
                            UserName = "admin@bhx.com",
                            Email = "admin@bhx.com",
                            FullName = "Quản Trị Viên Tối Cao",
                            Address = "Hệ Thống Bách Hóa Xanh",
                            EmailConfirmed = true
                        };
                        var createResult = userManager.Create(adminUser, "Admin123@");
                        if (createResult.Succeeded)
                        {
                            userManager.AddToRole(adminUser.Id, "Admin");
                        }
                    }
                    else
                    {
                        if (!userManager.IsInRole(adminUser.Id, "Admin"))
                        {
                            userManager.AddToRole(adminUser.Id, "Admin");
                        }
                    }
                }
            }
            catch { }
        }
    }
}

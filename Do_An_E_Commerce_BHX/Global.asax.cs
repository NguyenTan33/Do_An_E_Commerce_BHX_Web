using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Do_An_E_Commerce_BHX
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            System.Data.Entity.Database.SetInitializer<Do_An_E_Commerce_BHX.Models.ApplicationDbContext>(null);

            // Tự động chuyển đổi cột UserId trong bảng Carts sang NVARCHAR(128) và bổ sung cột GuestId trong SQL Server nếu chưa có
            try
            {
                using (var db = new Models.ApplicationDbContext())
                {
                    db.Database.ExecuteSqlCommand(@"
                        BEGIN TRY
                            ALTER TABLE dbo.Carts ALTER COLUMN UserId NVARCHAR(128) NULL;
                        END TRY
                        BEGIN CATCH
                        END CATCH;

                        BEGIN TRY
                            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Carts') AND name = 'GuestId')
                            BEGIN
                                ALTER TABLE dbo.Carts ADD GuestId NVARCHAR(128) NULL;
                            END
                        END TRY
                        BEGIN CATCH
                        END CATCH;
                    ");
                }
            }
            catch (Exception)
            {
                // Bỏ qua nếu đã chuyển đổi xong
            }

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}

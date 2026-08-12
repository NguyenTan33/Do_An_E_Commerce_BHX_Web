using System;
using System.Linq;

namespace Do_An_E_Commerce_BHX.Helpers
{
    public static class ManagerSensitiveInfoConfig
    {
        private static bool _allowManagerViewSensitiveInfo = false;
        private static readonly object _lockObj = new object();
        private static bool _isInitialized = false;

        public static bool AllowManagerViewSensitiveInfo
        {
            get
            {
                EnsureInitialized();
                return _allowManagerViewSensitiveInfo;
            }
            set
            {
                lock (_lockObj)
                {
                    _allowManagerViewSensitiveInfo = value;
                    SaveSettingToDb(value);
                }
            }
        }

        public static bool ShouldMaskForUser(System.Security.Principal.IPrincipal user)
        {
            if (user == null || !user.Identity.IsAuthenticated) return true;

            // Admin NEVER has info masked
            if (user.IsInRole("Admin")) return false;

            // Manager: check configuration flag
            if (user.IsInRole("Manager"))
            {
                return !AllowManagerViewSensitiveInfo;
            }

            // Other users: masked
            return true;
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;
            lock (_lockObj)
            {
                if (_isInitialized) return;
                try
                {
                    using (var db = new Models.ApplicationDbContext())
                    {
                        var setting = db.Database.SqlQuery<string>(
                            "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SystemSetting') SELECT SettingValue FROM SystemSetting WHERE SettingKey = 'AllowManagerViewSensitiveInfo' ELSE SELECT NULL"
                        ).FirstOrDefault();

                        if (!string.IsNullOrEmpty(setting))
                        {
                            bool val;
                            if (bool.TryParse(setting, out val))
                            {
                                _allowManagerViewSensitiveInfo = val;
                            }
                        }
                    }
                }
                catch { }
                _isInitialized = true;
            }
        }

        private static void SaveSettingToDb(bool value)
        {
            try
            {
                using (var db = new Models.ApplicationDbContext())
                {
                    string sql = @"
                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SystemSetting')
                        BEGIN
                            CREATE TABLE [dbo].[SystemSetting] (
                                [SettingKey] NVARCHAR(100) NOT NULL PRIMARY KEY,
                                [SettingValue] NVARCHAR(MAX) NULL,
                                [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                            );
                        END

                        IF EXISTS (SELECT 1 FROM [dbo].[SystemSetting] WHERE [SettingKey] = 'AllowManagerViewSensitiveInfo')
                            UPDATE [dbo].[SystemSetting] SET [SettingValue] = @p0, [UpdatedAt] = GETDATE() WHERE [SettingKey] = 'AllowManagerViewSensitiveInfo';
                        ELSE
                            INSERT INTO [dbo].[SystemSetting] ([SettingKey], [SettingValue], [UpdatedAt]) VALUES ('AllowManagerViewSensitiveInfo', @p0, GETDATE());
                    ";
                    db.Database.ExecuteSqlCommand(sql, value.ToString().ToLower());
                }
            }
            catch { }
        }
    }
}

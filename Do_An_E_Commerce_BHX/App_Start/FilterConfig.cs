using System.Web;
using System.Web.Mvc;

namespace Do_An_E_Commerce_BHX
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}

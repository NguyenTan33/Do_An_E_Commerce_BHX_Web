using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Do_An_E_Commerce_BHX.Startup))]
namespace Do_An_E_Commerce_BHX
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

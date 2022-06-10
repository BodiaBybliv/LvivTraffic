using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(LvivTraffic.Startup))]
namespace LvivTraffic
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

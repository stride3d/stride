using Microsoft.Owin;
using Owin;
using Xenko.Metrics.ServerApp;

[assembly: OwinStartup(typeof(Xenko.Metrics.ServerApp.Startup))]

namespace Xenko.Metrics.ServerApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseMetricServer();
        }
    }
}

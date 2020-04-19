using Microsoft.Owin;
using Owin;
using Stride.Metrics.ServerApp;

[assembly: OwinStartup(typeof(Stride.Metrics.ServerApp.Startup))]

namespace Stride.Metrics.ServerApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseMetricServer();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Stride.Metrics.ServerApp.Models;

namespace Stride.Metrics.ServerApp.Controllers
{
    /// <summary>
    /// ApiController that provides MVC like View method.
    /// </summary>
    public abstract class CustomApiControllerBase : ApiController
    {
        private readonly ViewRenderer.FakeController fakeController;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomApiControllerBase"/> class.
        /// </summary>
        protected CustomApiControllerBase()
        {
            fakeController = ViewRenderer.CreateController<ViewRenderer.FakeController>();
        }

        public dynamic ViewBag
        {
            get { return fakeController.ViewBag; }
        }

        protected async Task<List<T>> SqlToList<T>(string sqlQuery, params object[] parameters)
        {
            using (var db = new MetricDbContext())
            {
                return await db.Database.SqlQuery<T>(sqlQuery, parameters).ToListAsync();
            }
        }

        /// <summary>
        /// Renders the specified view using MVC Razor.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="model">The model.</param>
        /// <returns>The view content.</returns>
        /// <exception cref="System.ArgumentNullException">view</exception>
        protected HttpResponseMessage View(string view, object model = null)
        {
            if (view == null) throw new ArgumentNullException("view");
            var html = ViewRenderer.RenderView(view, null, fakeController.ControllerContext);
            return new HttpResponseMessage()
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
        }

        protected string GetIPAddress(HttpRequestMessage request = null)
        {
            try
            {
                request = request ?? Request;

                if (request.Properties.ContainsKey("MS_HttpContext"))
                {
                    var ip = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
                    if (ip == null)
                    {
                        var myRequest = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request;
                        ip = myRequest.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    }
                    return ip;
                }
                if (HttpContext.Current != null)
                {
                    return HttpContext.Current.Request.UserHostAddress;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }           
        }
    }
}
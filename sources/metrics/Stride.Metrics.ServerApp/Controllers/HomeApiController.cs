using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Xenko.Metrics.ServerApp.Controllers
{
    [RoutePrefix("")]
    internal class HomeApiController : CustomApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public HttpResponseMessage Index()
        {
            Trace.TraceInformation("/ Metrics dashboard view from {0}", GetIPAddress());

            return View("~/Views/Home/Index.cshtml");
        }
    }
}
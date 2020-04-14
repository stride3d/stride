using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Stride.Metrics.ServerApp.Controllers
{
    /// <summary>
    /// Filter that redirects to HTTPS automatically
    /// </summary>
    public class RequireHttpsAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            if (request.RequestUri.Scheme != Uri.UriSchemeHttps)
            {
                HttpResponseMessage response;
                if (request.Method.Equals(HttpMethod.Get) || request.Method.Equals(HttpMethod.Head))
                {
                    var uri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttps, Port = 443 };
                    response = new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = uri.Uri } };
                }
                else
                {
                    response = request.CreateResponse(HttpStatusCode.Forbidden);
                }

                actionContext.Response = response;
            }
        }
    }
}
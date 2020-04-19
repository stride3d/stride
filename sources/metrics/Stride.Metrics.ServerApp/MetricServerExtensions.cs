using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Optimization;
using Microsoft.Owin.Builder;
using Newtonsoft.Json.Serialization;
using Owin;
using Stride.Metrics.ServerApp.Content;
using Stride.Metrics.ServerApp.Controllers;
using Stride.Metrics.ServerApp.Models;
using Thinktecture.IdentityServer.AccessTokenValidation;

namespace Stride.Metrics.ServerApp
{
    public static class MetricServerExtensions
    {
        public static void UseMetricServer(this IAppBuilder metricsApp, string authorityServer = null)
        {
            //authorityServer = authorityServer ?? "http://localhost:44300/";

            BundleConfig.RegisterBundles(BundleTable.Bundles);

            MetricDbContext.Initialize();

            //metricsApp.UseIdentityServerBearerTokenAuthentication(new IdentityServerBearerTokenAuthenticationOptions
            //{
            //    Authority = authorityServer,
            //    RequiredScopes = new[]
            //    {
            //        "metrics.write"
            //    },
            //});

            var config = new HttpConfiguration();

            // Enable this to view webapi exception in details. Only for debug, not for production!
            //config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            // Remove XML and use 
#if !DEBUG
            config.Filters.Add(new RequireHttpsAttribute());
#endif
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            config.Services.Replace(typeof(IHttpControllerTypeResolver), new HttpControllerTypeResolver());
            config.MapHttpAttributeRoutes();
            metricsApp.UseWebApi(config);
        }

        private class HttpControllerTypeResolver : IHttpControllerTypeResolver
        {
            public ICollection<Type> GetControllerTypes(IAssembliesResolver _)
            {
                return new List<Type>() { typeof (HomeApiController), typeof(MetricApiController) };
            }
        }
    }
}
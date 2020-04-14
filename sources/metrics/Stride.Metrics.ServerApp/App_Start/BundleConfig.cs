using System.Web.Optimization;
using BundleTransformer.Core.Bundles;
using BundleTransformer.Core.Orderers;

namespace Stride.Metrics.ServerApp.Content
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/js").Include(
                // jquery
                "~/Scripts/jquery-{version}.js",
                // bootstrap
                "~/Scripts/bootstrap.js",
                // chart
                "~/Scripts/Chart.js",
                // angular
                "~/Scripts/angular.min.js",
                // parsley
                "~/Scripts/parsley.min.js",
                "~/Scripts/parsley.remote.min.js",
                // angular-chart
                "~/Scripts/angular-chart.js",
                // respond
                "~/Scripts/respond.js"
                ));

            //"~/Scripts/encoder.min.js",
            //"~/Scripts/identity_angular.js",

            //var commonStylesBundle = ;
            // commonStylesBundle.Include("~/Content/bootstrap/bootstrap.less");
            bundles.Add(new CustomStyleBundle(@"~/css") {Orderer = new NullOrderer()}.Include(
                "~/Content/bootstrap/bootstrap.less",
                "~/Content/angular-chart.less",
                "~/Scripts/angular-csp.css",
                "~/Content/font-awesome.css",
                "~/Content/site.css"));

            BundleTable.EnableOptimizations = true;
        }
    }
}

using Common.Bundles;
using System.Web;
using System.Web.Optimization;

namespace AdminDashboard
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Enable bundling optimizations, even when the site is in debug mode or local.
            BundleTable.EnableOptimizations = true;


            // Create the bundles
            var styles = new StyleBundle("~/bundles/styles");
            var icons = new StyleBundle("~/bundles/icons");
            var scripts = new ScriptBundle("~/bundles/scripts");


            // jQuery
            scripts.Include(
                "~/Content/scripts/vendor/jquery.js",
                "~/Content/scripts/vendor/jquery.extended.js",
                "~/Content/scripts/vendor/jquery.validate.js",
                "~/Content/scripts/vendor/jquery.validate.config.js",
                "~/Content/scripts/vendor/jquery.validate.unobtrusive.js",
                "~/Content/scripts/vendor/jquery.validate.unobtrusive.custom.js",
                "~/Content/scripts/vendor/jquery.unobtrusive-ajax.js");


            // Common styles
            styles.Include("~/Content/styles/common.css", new CssRewriteUrlTransformer());


            // Bootstrap
            scripts.Include("~/Content/scripts/vendor/bootstrap.js");


            // Icons
            icons.Include("~/Content/styles/icons.css");


            // Toastr
            styles.Include("~/Content/scripts/vendor/toastr/toastr.css");
            scripts.Include("~/Content/scripts/vendor/toastr/toastr.js");


            // Loading Buttons plugin styles
            styles.Include("~/Content/scripts/vendor/loadingbuttons/loadingbuttons.css", new CssRewriteUrlTransformer());
            scripts.Include(
                "~/Content/scripts/modules/widgets.js",
                "~/Content/scripts/vendor/loadingbuttons/loadingbuttons.js");


            // Kendo
            styles.Include("~/Content/scripts/vendor/kendo/styles/kendo.common-bootstrap.min.css", new CssRewriteUrlTransformer());
            styles.Include("~/Content/scripts/vendor/kendo/styles/kendo.bootstrap.min.css", new CssRewriteUrlTransformer());
            scripts.Include("~/Content/scripts/vendor/kendo/scripts/kendo.web.min.js");


            //// Fusion Charts
            //scripts.Include(
            //    "~/Content/scripts/vendor/fusioncharts/FusionCharts.js",
            //    "~/Content/scripts/vendor/fusioncharts/FusionCharts.jqueryplugin.js");


            // Scaffolding styles
            styles.Include("~/Content/styles/scaffolding.css", new CssRewriteUrlTransformer());


            // App-specific styles and scripts
            styles.Include("~/Content/styles/site.css", new CssRewriteUrlTransformer());
            scripts.Include("~/Content/scripts/app/extensions.js");


            bundles.Add(styles);
            bundles.Add(icons);
            bundles.Add(scripts);
        }

    }
}
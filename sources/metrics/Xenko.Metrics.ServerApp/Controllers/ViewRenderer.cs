using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Xenko.Metrics.ServerApp.Controllers
{
    /// <summary>
    /// Class that renders MVC views to a string using the
    /// standard MVC View Engine to render the view. 
    /// </summary>
    public class ViewRenderer
    {
        /// <summary>
        /// Required Controller Context
        /// </summary>
        protected ControllerContext Context { get; set; }

        /// <summary>
        /// Initializes the ViewRenderer with a Context.
        /// </summary>
        /// <param name="controllerContext">
        /// If you are running within the context of an ASP.NET MVC request pass in
        /// the controller's context. 
        /// Only leave out the context if no context is otherwise available.
        /// </param>
        public ViewRenderer(ControllerContext controllerContext = null)
        {
            // Create a known controller from HttpContext if no context is passed
            if (controllerContext == null)
            {
                if (HttpContext.Current != null)
                    controllerContext = CreateController<FakeController>().ControllerContext;
                else
                    throw new InvalidOperationException(
                        "ViewRenderer must run in the context of an ASP.NET " +
                        "Application and requires HttpContext.Current to be present.");
            }
            Context = controllerContext;
        }

        /// <summary>
        /// Renders a full MVC view to a string. Will render with the full MVC
        /// View engine including running _ViewStart and merging into _Layout        
        /// </summary>
        /// <param name="viewPath">
        /// The path to the view to render. Either in same controller, shared by 
        /// name or as fully qualified ~/ path including extension
        /// </param>
        /// <param name="model">The model to render the view with</param>
        /// <returns>String of the rendered view or null on error</returns>
        public string RenderView(string viewPath, object model)
        {
            return RenderViewToStringInternal(viewPath, model, false);
        }


        /// <summary>
        /// Renders a partial MVC view to string. Use this method to render
        /// a partial view that doesn't merge with _Layout and doesn't fire
        /// _ViewStart.
        /// </summary>
        /// <param name="viewPath">
        /// The path to the view to render. Either in same controller, shared by 
        /// name or as fully qualified ~/ path including extension
        /// </param>
        /// <param name="model">The model to pass to the viewRenderer</param>
        /// <returns>String of the rendered view or null on error</returns>
        public string RenderPartialView(string viewPath, object model)
        {
            return RenderViewToStringInternal(viewPath, model, true);
        }

        /// <summary>
        /// Renders a partial MVC view to string. Use this method to render
        /// a partial view that doesn't merge with _Layout and doesn't fire
        /// _ViewStart.
        /// </summary>
        /// <param name="viewPath">
        /// The path to the view to render. Either in same controller, shared by 
        /// name or as fully qualified ~/ path including extension
        /// </param>
        /// <param name="model">The model to pass to the viewRenderer</param>
        /// <param name="controllerContext">Active Controller context</param>
        /// <returns>String of the rendered view or null on error</returns>
        public static string RenderView(string viewPath, object model,
                                        ControllerContext controllerContext)
        {
            ViewRenderer renderer = new ViewRenderer(controllerContext);
            return renderer.RenderView(viewPath, model);
        }

        /// <summary>
        /// Renders a partial MVC view to string. Use this method to render
        /// a partial view that doesn't merge with _Layout and doesn't fire
        /// _ViewStart.
        /// </summary>
        /// <param name="viewPath">
        /// The path to the view to render. Either in same controller, shared by 
        /// name or as fully qualified ~/ path including extension
        /// </param>
        /// <param name="model">The model to pass to the viewRenderer</param>
        /// <param name="controllerContext">Active Controller context</param>
        /// <param name="errorMessage">optional out parameter that captures an error message instead of throwing</param>
        /// <returns>String of the rendered view or null on error</returns>
        public static string RenderView(string viewPath, object model,
                                        ControllerContext controllerContext,
                                        out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var renderer = new ViewRenderer(controllerContext);
                return renderer.RenderView(viewPath, model);
            }
            catch (Exception ex)
            {
                errorMessage = ex.GetBaseException().Message;
            }
            return null;
        }

        /// <summary>
        /// Renders a partial MVC view to string. Use this method to render
        /// a partial view that doesn't merge with _Layout and doesn't fire
        /// _ViewStart.
        /// </summary>
        /// <param name="viewPath">
        /// The path to the view to render. Either in same controller, shared by 
        /// name or as fully qualified ~/ path including extension
        /// </param>
        /// <param name="model">The model to pass to the viewRenderer</param>
        /// <param name="controllerContext">Active controller context</param>
        /// <returns>String of the rendered view or null on error</returns>
        public static string RenderPartialView(string viewPath, object model,
                                                ControllerContext controllerContext)
        {
            var renderer = new ViewRenderer(controllerContext);
            return renderer.RenderPartialView(viewPath, model);
        }

        /// <summary>
        /// Renders a partial MVC view to string. Use this method to render
        /// a partial view that doesn't merge with _Layout and doesn't fire
        /// _ViewStart.
        /// </summary>
        /// <param name="viewPath">
        /// The path to the view to render. Either in same controller, shared by 
        /// name or as fully qualified ~/ path including extension
        /// </param>
        /// <param name="model">The model to pass to the viewRenderer</param>
        /// <param name="controllerContext">Active controller context</param>
        /// <param name="errorMessage">optional output parameter to receive an error message on failure</param>
        /// <returns>String of the rendered view or null on error</returns>
        public static string RenderPartialView(string viewPath, object model,
                                                ControllerContext controllerContext,
                                                out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var renderer = new ViewRenderer(controllerContext);
                return renderer.RenderPartialView(viewPath, model);
            }
            catch (Exception ex)
            {
                errorMessage = ex.GetBaseException().Message;
            }
            return null;
        }

        /// <summary>
        /// Internal method that handles rendering of either partial or 
        /// or full views.
        /// </summary>
        /// <param name="viewPath">
        /// The path to the view to render. Either in same controller, shared by 
        /// name or as fully qualified ~/ path including extension
        /// </param>
        /// <param name="model">Model to render the view with</param>
        /// <param name="partial">Determines whether to render a full or partial view</param>
        /// <returns>String of the rendered view</returns>
        protected string RenderViewToStringInternal(string viewPath, object model,
                                                    bool partial = false)
        {
            // first find the ViewEngine for this view
            ViewEngineResult viewEngineResult = null;
            if (partial)
                viewEngineResult = ViewEngines.Engines.FindPartialView(Context, viewPath);
            else
                viewEngineResult = ViewEngines.Engines.FindView(Context, viewPath, null);

            if (viewEngineResult == null)
                throw new FileNotFoundException(string.Format("View cannot be found {0}", viewPath));

            // get the view and attach the model to view data
            var view = viewEngineResult.View;
            Context.Controller.ViewData.Model = model;

            string result = null;

            using (var sw = new StringWriter())
            {
                var ctx = new ViewContext(Context, view,
                                            Context.Controller.ViewData,
                                            Context.Controller.TempData,
                                            sw);
                view.Render(ctx, sw);
                result = sw.ToString();
            }

            return result;
        }


        /// <summary>
        /// Creates an instance of an MVC controller from scratch 
        /// when no existing ControllerContext is present       
        /// </summary>
        /// <typeparam name="T">Type of the controller to create</typeparam>
        /// <returns></returns>
        public static T CreateController<T>(RouteData routeData = null)
                    where T : ControllerBase, new()
        {
            T controller = new T();

            // Create an MVC Controller Context
            HttpContextBase wrapper = null;
            if (HttpContext.Current != null)
                wrapper = new HttpContextWrapper(System.Web.HttpContext.Current);
            //else
            //    wrapper = CreateHttpContextBase(writer);


            if (routeData == null)
                routeData = new RouteData();

            if (!routeData.Values.ContainsKey("controller") && !routeData.Values.ContainsKey("Controller"))
                routeData.Values.Add("controller", controller.GetType().Name
                                                            .ToLower()
                                                            .Replace("controller", ""));

            controller.ControllerContext = new ControllerContext(wrapper, routeData, controller);
            return controller;
        }

        public class FakeController : ControllerBase { protected override void ExecuteCore() { } }
    }
}
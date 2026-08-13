using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace TUI.WebPortal.AppCode
{

    public static partial class Extension
    {
        public static string GetDescription(this object obj)
        {
            return GetDescription(obj, false);
        }

        public static string GetDescription(this object obj, bool isTop)
        {
            if (obj == null) return string.Empty;
            try
            {
                Type enumType = obj.GetType();
                DescriptionAttribute da;
                if (isTop)
                {
                    da = (DescriptionAttribute)Attribute.GetCustomAttribute(enumType, typeof(DescriptionAttribute));
                }
                else
                {
                    var fi = enumType.GetField(Enum.GetName(enumType, obj));
                    da = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
                }
                if (da != null && !string.IsNullOrEmpty(da.Description))
                    return da.Description;
            }
            catch
            { }
            return obj.ToString();
        }




        public static async Task<string> RenderViewAsync<TModel>(this Controller controller, string viewName, TModel model, bool partial = false)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                IViewEngine viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !partial);

                if (viewResult.Success == false)
                {
                    return $"A view with the name {viewName} could not be found";
                }

                ViewContext viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request.Headers != null)
                return request.Headers["X-Requested-With"] == "XMLHttpRequest";
            return false;
        }
    }
}
namespace Microsoft.AspNetCore.Http.Extensions
{
    public static partial class Extension
    {
        public static string GetValue(this HttpRequest request, string key)
        {
            if (!string.IsNullOrEmpty(request.Query[key]))
            {
                return request.Query[key].ToString();
            }
            else if (request.HasFormContentType && request.Form != null && !string.IsNullOrEmpty(request.Form[key]))
            {
                return request.Form[key].ToString();
            }
            else return null;
        }
    }
    //public static class TempDataExtensions
    //{
    //    public static void Set<T>(this ITempDataDictionary tempData, string key, T value)
    //    {

    //       HttpContext.Current.Session.SetValue($"TempData_{key}", value);
    //    }

    //    public static T Get<T>(this ITempDataDictionary tempData, string key)
    //    {
    //        T obj = HttpContext.Current.Session.GetValue<T>($"TempData_{key}");
    //        HttpContext.Current.Session.Remove($"TempData_{key}");
    //        return obj;
    //        //object o;
    //        //tempData.TryGetValue(key, out o);
    //        //return o == null ? null : JsonConvert.DeserializeObject<T>((string)o);
    //    }
    //}
}
namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public static class HtmlHelperViewExtensions
    {
        public static String ToHtmlString(this IHtmlContent content, bool htmlEncode = true)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            using (System.IO.TextWriter tw = new System.IO.StringWriter(sb))
            {
                content.WriteTo(tw, HtmlEncoder.Default);
                var result = tw.ToString();
                if (htmlEncode == false)
                {
                    result = System.Web.HttpUtility.HtmlDecode(result);
                }
                return result;

            }
        }
        public static IHtmlContent PartialWithPrefix(this IHtmlHelper helper, string Prefix, string partialViewName, object model)
        {
            var viewData = new ViewDataDictionary(helper.ViewData);
            viewData.TemplateInfo.HtmlFieldPrefix = Prefix;
            return helper.PartialAsync(partialViewName, model, viewData).Result;

        }
        public static IHtmlContent PartialWithPrefix(this IHtmlHelper helper, string Prefix, string partialViewName, object model, ViewDataDictionary viewData)
        {
            // var viewData = new ViewDataDictionary(helper.ViewData);
            viewData.TemplateInfo.HtmlFieldPrefix = Prefix;
            return helper.PartialAsync(partialViewName, model, viewData).Result;

        }
        public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, object parameters = null, bool sync = false)
        {
            var controller = (string)helper.ViewContext.RouteData.Values["controller"];
            return RenderAction(helper, action, controller, parameters, sync);
        }

        public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, string controller, object parameters = null, bool sync = false)
        {
            var area = (string)helper.ViewContext.RouteData.Values["area"];
            return RenderAction(helper, action, controller, area, parameters, sync);
        }

        public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, string controller, string area, object parameters = null, bool sync = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(controller));
            if (controller == null)
                throw new ArgumentNullException(nameof(action));
            if (sync == true)
            {
                return RenderActionAsync(helper, action, controller, area, parameters).GetAwaiter().GetResult();
            }
            else
            {
                return RenderActionAsync(helper, action, controller, area, parameters).Result;
            }

        }

        private static async Task<IHtmlContent> RenderActionAsync(this IHtmlHelper helper, string action, string controller, string area, object parameters = null)
        {
            // fetching required services for invocation
            var currentHttpContext = helper.ViewContext.HttpContext;
            var httpContextFactory = GetServiceOrFail<IHttpContextFactory>(currentHttpContext);
            var actionInvokerFactory = GetServiceOrFail<IActionInvokerFactory>(currentHttpContext);
            var actionSelector = GetServiceOrFail<IActionDescriptorCollectionProvider>(currentHttpContext);

            // creating new action invocation context
            var routeData = new RouteData();
            var routeParams = new RouteValueDictionary(parameters ?? new { });
            var routeValues = new RouteValueDictionary(new { area, controller, action });
            var newHttpContext = httpContextFactory.Create(currentHttpContext.Features);

            using (newHttpContext.Response.Body = new MemoryStream())
            {
                foreach (var router in helper.ViewContext.RouteData.Routers)
                    routeData.PushState(router, null, null);

                routeData.PushState(null, routeValues, null);
                routeData.PushState(null, routeParams, null);
                ActionDescriptor actionDescriptor;
                var actionDescriptors = actionSelector.ActionDescriptors.Items.Where(i => i.RouteValues["Controller"] == controller && i.RouteValues["Action"] == action);
                if (actionDescriptors.Count() == 1) actionDescriptor = actionDescriptors.First();
                else actionDescriptor = actionDescriptors.First(i => i.RouteValues["area"] == area);
                var actionContext = new ActionContext(newHttpContext, routeData, actionDescriptor);

                // invoke action and retreive the response body
                var invoker = actionInvokerFactory.CreateInvoker(actionContext);
                string content = null;

                await invoker.InvokeAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        content = task.Exception.Message;
                    }
                    else if (task.IsCompleted)
                    {
                        newHttpContext.Response.Body.Position = 0;
                        using (var reader = new StreamReader(newHttpContext.Response.Body))
                        {
                            content = reader.ReadToEnd();
                            reader.Close();
                        }
                    }
                });
                newHttpContext.Response.Body.Close();
                return new HtmlString(content);
            }
        }

        private static TService GetServiceOrFail<TService>(HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            var service = httpContext.RequestServices.GetService(typeof(TService));

            if (service == null)
                throw new InvalidOperationException($"Could not locate service: {nameof(TService)}");

            return (TService)service;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace VueLib.Web.Services;

/// <summary>把 Razor 视图渲染为字符串（组件注册快照用）</summary>
public class ViewRenderService
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public ViewRenderService(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    /// <summary>渲染视图到字符串；viewPath 以 "~/" 或 "/" 开头表示绝对视图路径</summary>
    public async Task<string> RenderToStringAsync(string viewPath, object? model = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var view = FindView(actionContext, viewPath);
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };
        using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext, view, viewData,
            new TempDataDictionary(httpContext, _tempDataProvider), writer,
            new HtmlHelperOptions());
        await view.RenderAsync(viewContext);
        return writer.ToString();
    }

    private IView FindView(ActionContext actionContext, string viewPath)
    {
        var result = _viewEngine.GetView("~/", viewPath, isMainPage: true);
        if (result.Success) return result.View;
        // 回退：按相对路径找
        result = _viewEngine.FindView(actionContext, viewPath, isMainPage: true);
        if (result.Success) return result.View;
        var errors = string.Join("; ", result.SearchedLocations);
        throw new InvalidOperationException($"视图找不到: {viewPath} → {errors}");
    }
}
